using HarmonyLib;

namespace GTFOPause.Patches;

/// <summary>
/// While paused in a gameplay focus state, gameplay input reads as idle: no moving, looking or
/// shooting at frozen enemies (GTFO's guns are hitscan). The menu key and chat still work.
/// </summary>
[HarmonyPatch(typeof(InputMapper))]
internal static class InputPatches
{
    private static bool ShouldBlock(InputAction action) =>
        PauseState.IsPaused
        && !PauseRules.AllowedWhilePaused(action)
        && PauseRules.BlocksInputIn(FocusStateManager.CurrentState);

    [HarmonyPatch(nameof(InputMapper.DoGetButton))]
    [HarmonyPrefix]
    private static bool DoGetButton(InputAction action, ref bool __result) => Pass(action, ref __result);

    [HarmonyPatch(nameof(InputMapper.DoGetButtonDown))]
    [HarmonyPrefix]
    private static bool DoGetButtonDown(InputAction action, ref bool __result) => Pass(action, ref __result);

    [HarmonyPatch(nameof(InputMapper.DoGetButtonUp))]
    [HarmonyPrefix]
    private static bool DoGetButtonUp(InputAction action, ref bool __result) => Pass(action, ref __result);

    [HarmonyPatch(nameof(InputMapper.DoGetAxis))]
    [HarmonyPrefix]
    private static bool DoGetAxis(InputAction action, ref float __result)
    {
        if (!ShouldBlock(action))
            return true;
        __result = 0f;
        return false;
    }

    private static bool Pass(InputAction action, ref bool result)
    {
        if (!ShouldBlock(action))
            return true;
        result = false;
        return false;
    }
}
