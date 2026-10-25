using SNetwork;
using UnityEngine;

namespace GTFOPause;

/// <summary>Decides when to pause and resume. Driven by GTFO-API events and PauseController's per-frame tick.</summary>
internal static class PauseLogic
{
    private static int s_lastHumanCount = -1;
    private static ulong s_lastMaster;

    public static void OnFocusStateChanged(eFocusState oldState, eFocusState newState)
    {
        if (!PauseRules.InLevel)
            return;

        if (newState == eFocusState.GlobalPopupMessage)
        {
            // Popups such as "Leave expedition?" lead into transitions that wait on scaled time.
            ForceResume("popup opened");
            return;
        }

        if (PauseRules.IsMenu(newState))
        {
            if (PauseConfig.PauseOnMenu.Value && PauseRules.IsSolo)
                PauseState.Add(PauseReason.Menu);
        }
        else if (PauseRules.IsMenu(oldState))
        {
            PauseState.Remove(PauseReason.Menu);
        }
    }

    public static void OnGameStateChanged(eGameStateName oldState, eGameStateName newState) =>
        ForceResume($"game state {oldState} -> {newState}");

    public static void OnCheckpointReloaded() => ForceResume("checkpoint reloaded");

    public static void Tick()
    {
        WatchSession();

        if (Input.GetKeyDown(PauseConfig.PauseHotkey.Value) && PauseRules.HotkeyAllowedIn(FocusStateManager.CurrentState))
            OnHotkey();
    }

    public static void ForceResume(string why)
    {
        if (PauseState.Reasons == PauseReason.None)
            return;

        Logger.Info($"Force resume: {why}");
        PauseState.Clear();
    }

    private static void OnHotkey()
    {
        if (!PauseRules.InLevel)
            return;

        if (PauseRules.IsSolo)
        {
            // While paused (even by the menu) the hotkey resumes, so the game can run behind an open menu.
            if (PauseState.IsPaused)
                PauseState.Clear();
            else
                PauseState.Add(PauseReason.Hotkey);
            return;
        }

        Notify.Event("Multiplayer pause isn't available yet");
    }

    /// <summary>A pause never survives leaving the level, a join or leave, or a host change.</summary>
    private static void WatchSession()
    {
        int humans = PauseRules.HumanCount();
        ulong master = SNet.HasMaster && SNet.Master != null ? SNet.Master.Lookup : 0;
        bool sessionChanged = humans != s_lastHumanCount || master != s_lastMaster;
        s_lastHumanCount = humans;
        s_lastMaster = master;

        if (!PauseState.IsPaused)
            return;

        if (!PauseRules.InLevel)
            ForceResume("left the level");
        else if (sessionChanged)
            ForceResume("players or host changed");
    }
}
