namespace GTFOPause;

/// <summary>Player-facing feedback: lines in the game event log and a PAUSED banner in the interaction prompt.</summary>
internal static class Notify
{
    private const int BannerPriority = 100;
    private static bool s_bannerShown;

    /// <summary>Who the banner credits for the pause; null for a local solo pause.</summary>
    public static string PausedBy { get; set; }

    public static void Event(string message)
    {
        Logger.Info(message);
        if (!PauseConfig.ShowNotifications.Value)
            return;

        var layer = GuiManager.PlayerLayer;
        if (layer == null || layer.m_gameEventLog == null)
            return;

        layer.m_gameEventLog.AddLogItem(message, eGameEventChatLogType.GameEvent);
    }

    public static void UpdateBanner()
    {
        bool show = PauseState.IsPaused
            && PauseConfig.ShowNotifications.Value
            && !PauseRules.IsMenu(FocusStateManager.CurrentState);
        if (!show && !s_bannerShown)
            return;

        var layer = GuiManager.InteractionLayer;
        if (layer == null)
            return;

        if (show)
        {
            layer.SetMessage(BannerText(), ePUIMessageStyle.Message, BannerPriority);
            layer.MessageVisible = true;
            HideIntroBoxes(layer.m_message);
        }
        else
        {
            layer.MessageVisible = false;
        }
        s_bannerShown = show;
    }

    /// <summary>
    /// Showing a hidden message plays its intro: CoroutineManager.BlinkOut flashes white boxes over it,
    /// timed on Clock.Time, which stands still while paused. The boxes would stay up and hide the
    /// banner, so switch them off every frame (BlinkOut leaves them off when it finishes anyway).
    /// </summary>
    private static void HideIntroBoxes(PUI_InteractionPrompt prompt)
    {
        if (prompt == null)
            return;

        if (prompt.m_whiteBox != null && prompt.m_whiteBox.activeSelf)
            prompt.m_whiteBox.SetActive(false);
        if (prompt.m_whiteBoxWide != null && prompt.m_whiteBoxWide.activeSelf)
            prompt.m_whiteBoxWide.SetActive(false);
    }

    private static string BannerText()
    {
        string key = PauseConfig.PauseHotkey.Value.ToString();
        return PausedBy == null
            ? $"PAUSED - press {key} to resume"
            : $"PAUSED by {PausedBy} - press {key} to resume";
    }
}
