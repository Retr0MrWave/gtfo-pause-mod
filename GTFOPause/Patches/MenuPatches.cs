using CellMenu;
using HarmonyLib;
using UnityEngine;

namespace GTFOPause.Patches;

/// <summary>
/// Press-and-hold menu buttons (CM_TimedButton: Retire to Lobby, Exit expedition, settings Save...)
/// fill their hold timer in OnHoverUpdate and drain it in Update by Clock.Delta, which is 0 while
/// paused, so the hold would never finish. Run both on real frame time while paused.
/// </summary>
[HarmonyPatch(typeof(CM_TimedButton))]
internal static class MenuPatches
{
    [HarmonyPatch(nameof(CM_TimedButton.OnHoverUpdate))]
    [HarmonyPrefix]
    private static void OnHoverUpdatePrefix(out float __state) => __state = UseRealDelta();

    [HarmonyPatch(nameof(CM_TimedButton.OnHoverUpdate))]
    [HarmonyPostfix]
    private static void OnHoverUpdatePostfix(float __state) => RestoreDelta(__state);

    [HarmonyPatch(nameof(CM_TimedButton.Update))]
    [HarmonyPrefix]
    private static void UpdatePrefix(out float __state) => __state = UseRealDelta();

    [HarmonyPatch(nameof(CM_TimedButton.Update))]
    [HarmonyPostfix]
    private static void UpdatePostfix(float __state) => RestoreDelta(__state);

    /// <summary>Returns the Clock.Delta to restore, or -1 if it was left alone.</summary>
    private static float UseRealDelta()
    {
        if (!PauseState.IsPaused)
            return -1f;

        float saved = Clock.Delta;
        Clock.Delta = Mathf.Min(Time.unscaledDeltaTime, Time.maximumDeltaTime);
        return saved;
    }

    private static void RestoreDelta(float saved)
    {
        if (saved >= 0f)
            Clock.Delta = saved;
    }
}
