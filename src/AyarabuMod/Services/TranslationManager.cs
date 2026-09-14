using Utility.Notifications;

namespace AyarabuMod.Services;

/// <summary>后台加载字典；消息 hook 只访问快照，不执行磁盘或网络 I/O。</summary>
public sealed class TranslationManager : IDisposable
{
    private readonly TranslationCache _cache;
    private readonly CancellationTokenSource _lifetime = new();
    private readonly object _gate = new();
    private readonly Dictionary<int, Dictionary<string, string>> _adventures = new();
    private readonly Dictionary<int, Task> _loads = new();
    private Dictionary<string, string>? _names;
    private Task? _namesLoad;
    private Task? _manifestLoad;
    private int? _current;
    private bool _disposed;

    /// <summary>绑定一个固定 CDN 和语言的缓存。</summary>
    public TranslationManager(TranslationCache cache) => _cache = cache;

    /// <summary>启动人物名预加载。</summary>
    public void Warmup()
    {
        lock (_gate)
        {
            if (_disposed)
                return;
            if (_namesLoad == null || _namesLoad.IsCompleted)
                _namesLoad = Task.Run(() => LoadAsync(null));
        }
    }

    /// <summary>选择剧情并合并后台任务；可选等待由同步游戏入口执行。</summary>
    public void BeginAdventure(int id, bool asyncMode)
    {
        Task load;
        lock (_gate)
        {
            if (_disposed)
                return;
            _current = id;
            Warmup();
            if (!_loads.TryGetValue(id, out load!) || load.IsCompleted)
                _loads[id] = load = Task.Run(() => LoadAsync(id));
        }
        // 游戏入口为同步 API。只在显式关闭异步模式时进行有界等待。
        if (!asyncMode && !Task.WhenAll(load, _namesLoad!).Wait(TimeSpan.FromSeconds(10)))
        {
            Logger.Warn("Translation wait timed out; background loading continues");
            Toast.Warning("翻译服务", "等待剧情翻译超时，继续后台加载");
        }
    }

    /// <summary>分别查找人物名和当前剧情，不跨字典回退。</summary>
    public (string? Name, string? Message) Translate(string? name, string? message)
    {
        lock (_gate)
        {
            string? translatedName = name;
            string? translatedMessage = message;
            if (name != null && _names?.TryGetValue(name, out var n) == true && n != null)
                translatedName = n;
            if (
                message != null
                && _current is int id
                && _adventures.TryGetValue(id, out var table)
                && table.TryGetValue(message, out var m)
                && m != null
            )
                translatedMessage = m;
            return (translatedName, translatedMessage);
        }
    }

    private async Task LoadAsync(int? id)
    {
        try
        {
            var token = _lifetime.Token;
            string type = id.HasValue ? TranslationPaths.Adventures : TranslationPaths.Names;
            string? key = id.HasValue ? TranslationPaths.FormatId(id.Value) : null;
            var local = await _cache
                .LoadLocalAsync<Dictionary<string, string>>(type, key, token)
                .ConfigureAwait(false);
            Publish(id, local);
            Task manifest;
            lock (_gate)
            {
                if (_disposed)
                    return;
                if (_manifestLoad == null || (_manifestLoad.IsCompleted && _cache.Manifest == null))
                    _manifestLoad = _cache.FetchManifestAsync(token);
                manifest = _manifestLoad;
            }
            await manifest.ConfigureAwait(false);
            var remote = await _cache.LoadAsync(type, key, token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            Publish(id, remote);
        }
        catch (OperationCanceledException) { }
        catch (Exception e)
        {
            Logger.Warn($"Translation load failed: {e.Message}");
        }
    }

    private void Publish(int? id, Dictionary<string, string>? table)
    {
        if (table == null)
            return;
        lock (_gate)
        {
            if (_disposed)
                return;
            if (id.HasValue)
                _adventures[id.Value] = table;
            else
                _names = table;
        }
    }

    /// <summary>停止发布结果，取消请求并在后台任务结束后释放取消源。</summary>
    public void Dispose()
    {
        Task[] pending;
        lock (_gate)
        {
            if (_disposed)
                return;
            _disposed = true;
            pending = _loads
                .Values.Concat(new[] { _namesLoad, _manifestLoad }.OfType<Task>())
                .ToArray();
        }
        _lifetime.Cancel();
        _ = FinishAsync(pending);
    }

    private async Task FinishAsync(Task[] pending)
    {
        try
        {
            await Task.WhenAll(pending).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { }
        finally
        {
            _lifetime.Dispose();
        }
    }
}
