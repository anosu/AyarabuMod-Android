using System.Globalization;

namespace AyarabuMod.Services;

/// <summary>CDN 已包含 translations 前缀，路径构造不自动补齐它。</summary>
public static class TranslationPaths
{
    public const string Manifest = "manifest";
    public const string Names = "names";
    public const string Adventures = "adventures";

    /// <summary>保持原游戏七位十进制剧情 ID。</summary>
    public static string FormatId(int id) =>
        id >= 0
            ? id.ToString("D7", CultureInfo.InvariantCulture)
            : throw new ArgumentOutOfRangeException(nameof(id));

    private static string Relative(string type, string language, string? id)
    {
        ValidateSegment(language);
        if (type is Manifest or Names)
            return $"{language}/{type}.json";
        if (type != Adventures || id == null)
            throw new ArgumentException("A valid resource type and adventure ID are required.");
        if (!int.TryParse(id, NumberStyles.None, CultureInfo.InvariantCulture, out var number))
            throw new ArgumentException("Adventure ID must be a nonnegative integer.", nameof(id));
        return $"{language}/adventures/{FormatId(number)}.json";
    }

    private static void ValidateSegment(string value)
    {
        if (
            string.IsNullOrWhiteSpace(value)
            || value.Any(c =>
                !(c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' or '-')
            )
        )
            throw new ArgumentException("Language must contain ASCII letters, digits or hyphens.");
    }

    /// <summary>在配置的资源根地址下构造请求 URL。</summary>
    public static string BuildRemoteUrl(
        string cdn,
        string type,
        string language,
        string? id = null
    ) => $"{cdn.TrimEnd('/')}/{Relative(type, language, id)}";

    /// <summary>缓存按语言和资源隔离。</summary>
    public static string BuildCachePath(
        string cacheDir,
        string type,
        string language,
        string? id = null
    ) =>
        Path.Combine(
            cacheDir,
            Relative(type, language, id).Replace('/', Path.DirectorySeparatorChar)
        );
}
