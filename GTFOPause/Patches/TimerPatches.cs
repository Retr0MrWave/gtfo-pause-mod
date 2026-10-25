using HarmonyLib;

namespace GTFOPause.Patches;

/// <summary>
/// Keeps the expedition timer (results screen, speedrun timing) from counting paused time.
/// Same guard as Hikaria Core's PauseGame; a no-op if timeScale already froze it.
/// </summary>
[HarmonyPatch(typeof(GS_InLevel), nameof(GS_InLevel.Update))]
internal static class TimerPatches
{
    private static void Postfix()
    {
        if (PauseState.IsPaused)
            Clock.ExpeditionProgressionTime = PauseState.FrozenExpeditionTime;
    }
}
