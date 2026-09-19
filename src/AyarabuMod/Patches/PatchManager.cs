using HarmonyLib;

namespace AyarabuMod.Patches;

/// <summary>
/// Harmony 补丁管理器。集中注册与卸载所有子补丁类，失败时回滚。
/// 子补丁类不持有静态状态，因此没有 <c>Reset</c> 步骤。
/// </summary>
internal static class PatchManager
{
    private static HarmonyLib.Harmony? _harmony;

    /// <summary>创建并注册所有 Harmony 补丁。</summary>
    internal static void Initialize()
    {
        if (_harmony != null)
            return;

        var harmony = new HarmonyLib.Harmony(ModInfo.Name);
        try
        {
            harmony.PatchAll(typeof(AdvVoicePatch));
            harmony.PatchAll(typeof(TranslationPatch));
            _harmony = harmony;
        }
        catch
        {
            try
            {
                harmony.UnpatchSelf();
            }
            catch (Exception e)
            {
                Logger.Error($"Harmony 回滚失败: {e}");
            }
            throw;
        }
    }

    /// <summary>卸载全部补丁。</summary>
    internal static void Shutdown()
    {
        var harmony = _harmony;
        _harmony = null;
        harmony?.UnpatchSelf();
    }
}
