using System;
using UnityEngine;

namespace GTFOPause;

[Flags]
internal enum PauseReason
{
    None = 0,
    /// <summary>Solo: the Esc menu is open.</summary>
    Menu = 1,
    /// <summary>Solo: toggled with the hotkey.</summary>
    Hotkey = 2,
    /// <summary>Multiplayer: the host says the squad is paused.</summary>
    Remote = 4,
}

/// <summary>
/// The freeze itself. The game is paused while any reason is set. Pausing is what Risk of Rain 2 does:
/// Time.timeScale = 0 (GTFO's Clock accumulates Time.deltaTime, so every gameplay timer stops with it),
/// plus suspending Wwise, which ignores timeScale.
/// </summary>
internal static class PauseState
{
    private static PauseReason s_reasons;
    private static float s_savedTimeScale = 1f;

    public static bool IsPaused { get; private set; }
    public static PauseReason Reasons => s_reasons;

    /// <summary>Clock.ExpeditionProgressionTime when the pause began; pinned while paused.</summary>
    public static float FrozenExpeditionTime { get; private set; }

    public static void Add(PauseReason reason) => Set(s_reasons | reason);
    public static void Remove(PauseReason reason) => Set(s_reasons & ~reason);
    public static void Clear() => Set(PauseReason.None);

    private static void Set(PauseReason reasons)
    {
        s_reasons = reasons;
        bool pause = reasons != PauseReason.None;
        if (pause == IsPaused)
            return;

        IsPaused = pause;
        if (pause)
        {
            s_savedTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale = 0f;
            FrozenExpeditionTime = Clock.ExpeditionProgressionTime;
            SetAudioSuspended(true);
            Logger.Info($"Paused ({reasons})");
        }
        else
        {
            Time.timeScale = s_savedTimeScale;
            SetAudioSuspended(false);
            Logger.Info("Resumed");
        }
    }

    private static void SetAudioSuspended(bool suspend)
    {
        try
        {
            // renderAnyway = false stops audio processing entirely, so sounds resume where they stopped.
            var result = suspend ? AkSoundEngine.Suspend(false) : AkSoundEngine.WakeupFromSuspend();
            if (result != AKRESULT.AK_Success)
                Logger.Warn($"Wwise {(suspend ? "Suspend" : "WakeupFromSuspend")} returned {result}");
        }
        catch (Exception e)
        {
            Logger.Error($"Wwise {(suspend ? "Suspend" : "WakeupFromSuspend")} failed: {e}");
        }
    }
}
