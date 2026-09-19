using MelonLoader;
using MelonLoader.Utils;
using Utility.Notifications;

namespace AyarabuMod;

/// <summary>全局配置：初始化所有 MelonPreferences 配置项并绑定变更通知。</summary>
internal static class Config
{
    internal const string DefaultCdn =
        "https://raw.githubusercontent.com/anosu/ayarabu-translation/refs/heads/main/translations";

    internal static readonly string FilePath = Path.Combine(
        MelonEnvironment.UserDataDirectory,
        $"{ModInfo.Name}.cfg"
    );

    internal static MelonPreferences_Entry<bool> Enabled = null!;
    internal static MelonPreferences_Entry<string> Cdn = null!;
    internal static MelonPreferences_Entry<string> Language = null!;
    internal static MelonPreferences_Entry<bool> AsyncMode = null!;
    internal static MelonPreferences_Entry<bool> VoiceInterruption = null!;

    private static bool _initializing;
    private static bool _entriesBound;
    private static MelonPreferences_Category _primary = null!;
    private static readonly List<MelonPreferences_Category> Categories = new();

    internal static void Initialize()
    {
        _initializing = true;
        try
        {
            if (!_entriesBound)
            {
                BindAllEntries();
                _entriesBound = true;
            }
            _primary.LoadFromFile(false);
            foreach (var category in Categories)
                category.SaveToFile(false);
        }
        finally
        {
            _initializing = false;
        }
    }

    private static void BindAllEntries()
    {
        var translation = CreateCategory("Ayarabu.Translation");
        Enabled = CreateEntry(
            translation,
            "Enabled",
            true,
            "启用人物名和剧情翻译；修改配置后重启游戏"
        );
        Cdn = CreateEntry(
            translation,
            "CDN",
            DefaultCdn,
            "翻译资源根地址，必须自行包含 /translations 前缀"
        );
        Language = CreateEntry(translation, "Language", "zh-Hans", "翻译语言标签");
        AsyncMode = CreateEntry(
            translation,
            "AsyncMode",
            false,
            "默认同步加载剧情翻译，入口最多等待 10 秒；开启后改为后台加载"
        );

        var adventure = CreateCategory("Ayarabu.Adventure");
        VoiceInterruption = CreateEntry(
            adventure,
            "VoiceInterruption",
            false,
            "剧情中播放下一段无声文本时是否中断当前角色语音"
        );
    }

    private static MelonPreferences_Category CreateCategory(string name)
    {
        var category = MelonPreferences.CreateCategory(name);
        category.SetFilePath(FilePath, false, false);
        _primary ??= category;
        Categories.Add(category);
        return category;
    }

    private static MelonPreferences_Entry<T> CreateEntry<T>(
        MelonPreferences_Category category,
        string key,
        T defaultValue,
        string description
    )
    {
        var entry = category.CreateEntry(key, defaultValue, description, description);
        entry.OnEntryValueChanged.Subscribe(
            (_, newValue) =>
            {
                if (_initializing)
                    return;
                category.SaveToFile(false);
                Core.Log?.Msg($"[{category.Identifier}] {key} => {newValue}");
                Toast.Info($"[{category.Identifier}]", $"{key} => {newValue}");
            }
        );
        return entry;
    }
}
