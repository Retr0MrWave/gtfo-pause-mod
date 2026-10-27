using HarmonyLib;
using SNetwork;

namespace GTFOPause.Patches;

/// <summary>
/// Retire to Lobby (a session command) and Exit expedition (leaving the hub) can be confirmed from
/// the menu while paused. Resume first, so the transition that follows doesn't run at timeScale 0.
/// </summary>
[HarmonyPatch]
internal static class SessionPatches
{
    [HarmonyPatch(typeof(SNet_SyncManager), nameof(SNet_SyncManager.SessionCommand))]
    [HarmonyPrefix]
    private static void SessionCommand(eSessionCommandType type) =>
        PauseLogic.ForceResume($"session command {type}");

    [HarmonyPatch(typeof(SNet_SessionHub), nameof(SNet_SessionHub.LeaveHub))]
    [HarmonyPrefix]
    private static void LeaveHub() => PauseLogic.ForceResume("leaving the session");
}
