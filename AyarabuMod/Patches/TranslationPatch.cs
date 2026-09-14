using HarmonyLib;
using Il2CppApp;
using Il2CppGameFrame;

namespace AyarabuMod.Patches;

[HarmonyPatch]
internal static class TranslationPatch
{
    [HarmonyPatch(typeof(AdventureTask), nameof(AdventureTask.createTask))]
    [HarmonyPrefix]
    private static void Begin(AdventureId __1)
    {
        try
        {
            Core.Translation?.BeginAdventure((int)__1, Config.AsyncMode.Value);
        }
        catch (Exception e)
        {
            Logger.Warn($"Adventure translation failed: {e.Message}");
        }
    }

    [HarmonyPatch(
        typeof(AdvMessage),
        nameof(AdvMessage.initialize),
        new[] { typeof(MainFrame), typeof(AdvUIView), typeof(AdvMessage.MessageCreationData) }
    )]
    [HarmonyPrefix]
    private static void Message(AdvMessage.MessageCreationData __2)
    {
        try
        {
            if (__2 == null || Core.Translation == null)
                return;
            var translated = Core.Translation.Translate(__2.NameText, __2.MessageText);
            __2.NameText = translated.Name;
            __2.MessageText = translated.Message;
        }
        catch (Exception e)
        {
            Logger.Warn($"Message translation failed: {e.Message}");
        }
    }
}
