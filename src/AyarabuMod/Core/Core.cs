using AyarabuMod.Patches;
using AyarabuMod.Services;
using MelonLoader;
using MelonLoader.Utils;
using Utility.Diagnostics;
using Utility.Notifications;

[assembly: MelonInfo(
    typeof(AyarabuMod.Core),
    AyarabuMod.ModInfo.Name,
    AyarabuMod.ModInfo.Version,
    AyarabuMod.ModInfo.Author
)]
[assembly: HarmonyDontPatchAll]

namespace AyarabuMod;

/// <summary>Ayarabu 的 LemonLoader Android 模组入口。</summary>
public sealed class Core : MelonMod
{
    private HttpClient? _client;
    internal static TranslationManager? Translation;
    internal static MelonLogger.Instance? Log;

    /// <summary>初始化日志转发、配置与补丁，随后按需创建翻译会话。</summary>
    public override void OnInitializeMelon()
    {
        Log = LoggerInstance;
        try
        {
            InitializeUtility();
            Config.Initialize();
            PatchManager.Initialize();
            if (!Config.Enabled.Value)
            {
                Toast.Info(ModInfo.Name, "Mod 已加载，翻译功能已关闭");
                return;
            }
            _client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            Translation = new TranslationManager(
                new TranslationCache(
                    Config.Cdn.Value,
                    Path.Combine(MelonEnvironment.UserDataDirectory, "AyarabuMod", "translations"),
                    Config.Language.Value,
                    _client
                )
            );
            Translation.Warmup();
            Logger.Info(
                $"{ModInfo.Name} {ModInfo.Version} loaded; AdventureTask.createTask / AdvMessage.initialize patched"
            );
            Toast.Success(ModInfo.Name, $"Mod 加载成功，版本：{ModInfo.Version}", duration: 7f);
        }
        catch
        {
            OnDeinitializeMelon();
            throw;
        }
    }

    /// <summary>卸载补丁并取消后台加载。</summary>
    public override void OnDeinitializeMelon()
    {
        PatchManager.Shutdown();
        Translation?.Dispose();
        Translation = null;
        _client?.Dispose();
        _client = null;
        Toast.Shutdown();
        Logging.SetSink(null);
    }

    private static void InitializeUtility()
    {
        Logging.SetSink(entry =>
        {
            string message = $"[{entry.Category}] {entry.Message}";
            if (entry.Exception != null)
                message += $"\n{entry.Exception}";
            if (entry.Level == LogLevel.Error)
                Log?.Error(message);
            else if (entry.Level == LogLevel.Warning)
                Log?.Warning(message);
            else
                Log?.Msg(message);
        });
        Toast.Initialize("AyarabuMod.ToastManager");
    }
}
