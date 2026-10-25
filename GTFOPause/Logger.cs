using BepInEx.Logging;

namespace GTFOPause;

internal static class Logger
{
    private static ManualLogSource s_log;

    public static void Setup(ManualLogSource log) => s_log = log;

    public static void Info(object data) => s_log.LogInfo(data);
    public static void Warn(object data) => s_log.LogWarning(data);
    public static void Error(object data) => s_log.LogError(data);
    public static void Debug(object data) => s_log.LogDebug(data);
}
