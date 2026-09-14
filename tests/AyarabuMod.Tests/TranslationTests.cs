using System.Net;
using System.Text;
using AyarabuMod.Services;
using Xunit;

namespace AyarabuMod
{
    internal static class Logger
    {
        internal static void Info(string message) { }

        internal static void Warn(string message) { }
    }
}

namespace AyarabuMod.Tests
{
    public sealed class TranslationTests : IDisposable
    {
        private readonly string _root = Path.Combine(
            Path.GetTempPath(),
            "Ayarabu.Tests",
            Guid.NewGuid().ToString("N")
        );
        private const string Json = "{\"名前\":\"姓名\",\"台詞\":\"台词\"}";

        [Theory]
        [InlineData(
            "https://host/translations/",
            "https://host/translations/zh-Hans/adventures/0000123.json"
        )]
        [InlineData("https://host/custom", "https://host/custom/zh-Hans/adventures/0000123.json")]
        public void CdnIsUsedVerbatim(string cdn, string expected) =>
            Assert.Equal(
                expected,
                TranslationPaths.BuildRemoteUrl(cdn, "adventures", "zh-Hans", "123")
            );

        [Theory]
        [InlineData("../zh-Hans")]
        [InlineData("zh_Hans")]
        [InlineData("")]
        public void InvalidLanguageCannotEscapeCache(string language) =>
            Assert.Throws<ArgumentException>(() =>
                TranslationPaths.BuildCachePath(_root, "names", language)
            );

        [Fact]
        public async Task ConcurrentRequestsDownloadOnceAndReuseValidatedDisk()
        {
            int requests = 0;
            using var client = Client(path =>
            {
                Interlocked.Increment(ref requests);
                return path.EndsWith("manifest.json")
                    ? Ok("{\"names\":\"1c270dad5856196dae8a51b3314e7448\"}")
                    : Ok(Json);
            });
            var cache = Cache(client);
            await cache.FetchManifestAsync();
            var results = await Task.WhenAll(
                Enumerable.Range(0, 20).Select(_ => cache.LoadAsync("names"))
            );
            Assert.All(results, result => Assert.Equal("姓名", result!["名前"]));
            Assert.Equal(2, requests);
            var next = Cache(client);
            await next.FetchManifestAsync();
            Assert.Equal("姓名", (await next.LoadAsync("names"))!["名前"]);
            Assert.Equal(3, requests);
        }

        [Theory]
        [InlineData("not json")]
        [InlineData("{\"名前\":\"wrong hash\"}")]
        public async Task InvalidRemotePreservesLocalCache(string remote)
        {
            Directory.CreateDirectory(Path.Combine(_root, "zh-Hans"));
            var path = Path.Combine(_root, "zh-Hans", "names.json");
            await File.WriteAllTextAsync(path, Json);
            using var client = Client(p =>
                Ok(p.EndsWith("manifest.json") ? "{\"names\":\"unmatched\"}" : remote)
            );
            var cache = Cache(client);
            await cache.FetchManifestAsync();
            Assert.Equal("姓名", (await cache.LoadAsync("names"))!["名前"]);
            Assert.Equal(Json, await File.ReadAllTextAsync(path));
        }

        [Fact]
        public async Task MissingRemoteUsesOfflineCache()
        {
            Directory.CreateDirectory(Path.Combine(_root, "zh-Hans"));
            await File.WriteAllTextAsync(Path.Combine(_root, "zh-Hans", "names.json"), Json);
            using var client = Client(_ => new(HttpStatusCode.NotFound));
            Assert.Equal("姓名", (await Cache(client).LoadAsync("names"))!["名前"]);
        }

        [Fact]
        public async Task MissingResourceRetriesAfterCooldownAndCancellationDoesNotWrite()
        {
            int count = 0;
            using var client = Client(_ => ++count == 1 ? new(HttpStatusCode.NotFound) : Ok(Json));
            var cache = new TranslationCache(
                "https://host/translations",
                _root,
                "zh-Hans",
                client,
                TimeSpan.Zero
            );
            Assert.Null(await cache.LoadAsync("names"));
            Assert.NotNull(await cache.LoadAsync("names"));
            using var canceled = new CancellationTokenSource();
            canceled.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                cache.LoadAsync("adventures", "0000002", canceled.Token)
            );
            Assert.False(File.Exists(Path.Combine(_root, "zh-Hans", "adventures", "0000002.json")));
        }

        [Fact]
        public void NamesAndAdventureRemainSeparateAndSwitchDoesNotLeakPreviousStory()
        {
            using var client = Client(p =>
                p.EndsWith("manifest.json") ? Ok("{}")
                : p.EndsWith("names.json") ? Ok("{\"same\":\"姓名\"}")
                : p.EndsWith("0000001.json") ? Ok("{\"same\":\"剧情一\"}")
                : new(HttpStatusCode.NotFound)
            );
            using var manager = new TranslationManager(Cache(client));
            manager.BeginAdventure(1, false);
            Assert.Equal(("姓名", "剧情一"), manager.Translate("same", "same"));
            manager.BeginAdventure(2, false);
            Assert.Equal(("姓名", "same"), manager.Translate("same", "same"));
            manager.BeginAdventure(1, false);
            Assert.Equal(("姓名", "剧情一"), manager.Translate("same", "same"));
        }

        private TranslationCache Cache(HttpClient client) =>
            new("https://host/translations", _root, "zh-Hans", client);

        private static HttpClient Client(Func<string, HttpResponseMessage> respond) =>
            new(new Handler(respond));

        private static HttpResponseMessage Ok(string json) =>
            new(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            };

        private sealed class Handler(Func<string, HttpResponseMessage> respond) : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken
            ) => Task.FromResult(respond(request.RequestUri!.AbsolutePath));
        }

        public void Dispose()
        {
            if (Directory.Exists(_root))
                Directory.Delete(_root, true);
        }
    }
}
