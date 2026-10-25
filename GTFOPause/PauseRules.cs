using SNetwork;

namespace GTFOPause;

internal static class PauseRules
{
    public static bool InLevel => GameStateManager.CurrentStateName == eGameStateName.InLevel;

    /// <summary>Host and the only non-bot player in the lobby: pauses need no network round-trip.</summary>
    public static bool IsSolo => SNet.IsMaster && HumanCount() <= 1;

    public static int HumanCount()
    {
        var players = SNet.LobbyPlayers;
        if (players == null)
            return 0;

        int count = 0;
        for (int i = 0; i < players.Count; i++)
        {
            var player = players[i];
            if (player != null && !player.IsBot)
                count++;
        }
        return count;
    }

    public static bool IsMenu(eFocusState state) => state == eFocusState.MainMenu;

    /// <summary>States where the hotkey could collide with typing or dev tools.</summary>
    public static bool HotkeyAllowedIn(eFocusState state) => state is not (
        eFocusState.FPS_TypingInChat or
        eFocusState.ComputerTerminal or
        eFocusState.Console or
        eFocusState.DebugMenu);

    /// <summary>Gameplay states where a paused player must not move, look or shoot.</summary>
    public static bool BlocksInputIn(eFocusState state) => state is
        eFocusState.FPS or
        eFocusState.FPS_CommunicationDialog or
        eFocusState.Hacking;

    /// <summary>Actions still allowed while paused in a gameplay state: open the menu and use chat.</summary>
    public static bool AllowedWhilePaused(InputAction action) => action is
        InputAction.MenuToggle or
        InputAction.TextChatOpenSend or
        InputAction.TextChatClose or
        InputAction.VoiceChatPushToTalk;
}
