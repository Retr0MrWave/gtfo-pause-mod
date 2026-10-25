using BepInEx.Configuration;
using UnityEngine;

namespace GTFOPause;

internal static class PauseConfig
{
    public static ConfigEntry<bool> PauseOnMenu { get; private set; }
    public static ConfigEntry<KeyCode> PauseHotkey { get; private set; }
    public static ConfigEntry<bool> ShowNotifications { get; private set; }
    public static ConfigEntry<bool> AllowMultiplayerPause { get; private set; }

    public static void Bind(ConfigFile config)
    {
        PauseOnMenu = config.Bind("General", "PauseOnMenu", true,
            "Solo only: pause while the Esc menu is open, like Risk of Rain 2 singleplayer.");
        PauseHotkey = config.Bind("General", "PauseHotkey", KeyCode.F8,
            "Toggles the pause. Solo: pauses without opening the menu. Multiplayer: asks the host to pause everyone.");
        ShowNotifications = config.Bind("General", "ShowNotifications", true,
            "Show the PAUSED banner and the paused/resumed lines in the game event log.");
        AllowMultiplayerPause = config.Bind("Multiplayer", "AllowMultiplayerPause", true,
            "Host only: let any player with GTFOPause pause the whole squad. Every human player needs the mod. Turn off to keep pausing solo-only.");
    }
}
