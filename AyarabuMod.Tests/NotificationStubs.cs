namespace Utility.Notifications;

// Host-independent tests exercise translation behavior without creating Unity objects.
internal static class Toast
{
    internal static bool Warning(string title, string message) => true;

    internal static bool Error(string title, string message) => true;
}
