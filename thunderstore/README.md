# GTFOPause

Pause GTFO the way Risk of Rain 2 does.

- **Solo** (you plus bots): the game freezes while the Esc menu is open. **F8** pauses without opening the menu.
- **Multiplayer**: any player can press **F8** to pause the whole squad, and any player can press it again to resume. Everyone sees who paused. The host can turn this off with `AllowMultiplayerPause`.

While the game is paused, enemies, bots, timers, reactor countdowns, alarms and the expedition timer all stop. Game audio freezes and picks up where it left off. Voice chat keeps working.

## Multiplayer

- **Every human player needs GTFOPause.** If anyone is missing it, the pause is refused and the game names who.
- The pause ends on its own when a player joins or leaves, the host changes, or the level ends.
- If a client loses contact with the host while paused, it resumes after a few seconds.

## Config

`BepInEx/config/retr0mrwave.GTFOPause.cfg` (or the r2modman config editor):

| Setting | Default | What it does |
|---|---|---|
| `General.PauseOnMenu` | `true` | Solo: pause while the Esc menu is open |
| `General.PauseHotkey` | `F8` | Toggle the pause |
| `General.ShowNotifications` | `true` | PAUSED banner and paused/resumed messages |
| `Multiplayer.AllowMultiplayerPause` | `true` | Host only: let players pause the squad |

## Notes

- While paused outside the menu you can't move, look or shoot. Esc and chat still work.
- Menu sounds are silent during a pause, because all game audio is suspended.
