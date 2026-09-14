using MelonLoader;
using MelonLoader.Utils;

namespace AyarabuMod;

internal static class Config
{
    internal const string DefaultCdn =
        "https://raw.githubusercontent.com/anosu/ayarabu-translation/refs/heads/main/translations";
    internal static MelonPreferences_Entry<bool> Enabled = null!;
    internal static MelonPreferences_Entry<bool> AsyncMode = null!;
    internal static MelonPreferences_Entry<string> Cdn = null!;
    internal static MelonPreferences_Entry<string> Language = null!;

    internal static void Initialize()
    {
        var category = MelonPreferences.CreateCategory("Ayarabu.Translation");
        category.SetFilePath(
            Path.Combine(MelonEnvironment.UserDataDirectory, "AyarabuMod.cfg"),
            false,
            false
        );
        Enabled = category.CreateEntry(
            "Enabled",
            true,
            description: "启用人物名和剧情翻译；修改配置后重启游戏"
        );
        Cdn = category.CreateEntry(
            "CDN",
            DefaultCdn,
            description: "翻译资源根地址，必须自行包含 /translations 前缀"
        );
        Language = category.CreateEntry("Language", "zh-Hans", description: "翻译语言标签");
        AsyncMode = category.CreateEntry(
            "AsyncMode",
            false,
            description: "默认同步加载剧情翻译，入口最多等待 10 秒；开启后改为后台加载"
        );
        category.LoadFromFile(false);
        category.SaveToFile(false);
    }
}
