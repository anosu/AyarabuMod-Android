using System;
using HarmonyLib;
using Il2CppApp;

namespace AyarabuMod.Patches;

/// <summary>
/// 剧情语音中断控制。<c>VoiceInterruption</c> 关闭时，点击推进到没有语音的台词不会切断上一条语音，
/// 脚本层也不再等待语音播完；自动模式保持原语义（等语音播完再推进），剧情结束时主动停掉残留语音。
/// </summary>
[HarmonyPatch]
internal static class AdvVoicePatch
{
    /// <summary>
    /// 脚本层的“等语音播完”门禁。<c>AdvEventScriptCtrl</c> 的读台词命令每帧轮询
    /// <c>isPlayVoice</c>，语音没播完就不返回；语音正是被点击打断后才从登记表里摘掉的。
    /// 关闭语音中断时直接报告语音已结束，让脚本立刻放行；自动模式保留原语义。
    /// </summary>
    [HarmonyPatch(
        typeof(AdvMessageScreen),
        nameof(AdvMessageScreen.isPlayVoice),
        new[] { typeof(SoundVoiceId) }
    )]
    [HarmonyPrefix]
    private static bool SkipVoiceWait(AdvMessageScreen __instance, ref bool __result)
    {
        try
        {
            if (Config.VoiceInterruption.Value || __instance == null || __instance.m_IsAutoMode)
                return true;

            __result = false;
            return false;
        }
        catch
        {
            return true;
        }
    }

    /// <summary>
    /// 点击推进时的语音打断。这个方法只有 <c>readMessageByTap</c> 一个调用点。
    /// 换行时的正常切断在 <c>AdvMessage.main</c> 里另有一份，用的是真实声道查询，不受影响。
    /// </summary>
    [HarmonyPatch(typeof(AdvMessageScreen), nameof(AdvMessageScreen.stopVoice), new Type[0])]
    [HarmonyPrefix]
    private static bool InterruptVoiceOnTap() => Config.VoiceInterruption.Value;

    /// <summary>
    /// 剧情结束时的残留语音。脚本不再等语音后，最后一条语音可能还在播就已经退出剧情，
    /// 而 <c>AdvMessage.release</c> 只清空登记表不停声道，所以在消息屏销毁前补一次停止。
    /// </summary>
    [HarmonyPatch(typeof(AdvMessageScreen), nameof(AdvMessageScreen.release), new Type[0])]
    [HarmonyPrefix]
    private static void StopResidualVoice(AdvMessageScreen __instance)
    {
        if (Config.VoiceInterruption.Value)
            return;

        try
        {
            var appSound = __instance?.m_MainFrame?.m_App?.m_AppSound;
            if (appSound != null && appSound.isPlayVOICE(-1))
                appSound.stopVOICE(0f, -1);
        }
        catch (Exception e)
        {
            Logger.Warn($"停止残留剧情语音失败: {e.Message}");
        }
    }
}
