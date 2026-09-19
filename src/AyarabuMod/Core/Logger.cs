namespace AyarabuMod;

/// <summary>统一日志封装，避免各处直接引用 Core.Log。</summary>
internal static class Logger
{
    internal static void Info(string message) => Core.Log?.Msg(message);

    internal static void Warn(string message) => Core.Log?.Warning(message);

    internal static void Error(string message) => Core.Log?.Error(message);
}
