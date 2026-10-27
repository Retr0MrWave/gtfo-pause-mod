# GTFOPause

Pause GTFO the way Risk of Rain 2 does.

- **Solo** (you plus bots): **F8** freezes the game, and pressing it again resumes. Esc opens the menu as usual and keeps working while paused. Turn on `PauseOnMenu` to also pause whenever the Esc menu is open.
- **Multiplayer**: any player can press **F8** to pause the whole squad, and any player can press it again to resume. Everyone sees who paused. The host can turn this off with `AllowMultiplayerPause`.

While the game is paused, enemies, bots, timers, reactor countdowns, alarms and the expedition timer all stop. Game audio freezes and picks up where it left off. Voice chat keeps working.

## Multiplayer

- **Every human player needs GTFOPause.** If anyone is missing it, the pause is refused and the game names who.
- The pause ends on its own when a player joins or leaves, the host changes, or the level ends.
- If a client loses contact with the host while paused, it resumes after a few seconds.

## Config

`BepInEx/config/mrwave.GTFOPause.cfg` (or the r2modman config editor):

| Setting | Default | What it does |
|---|---|---|
| `General.PauseOnMenu` | `false` | Solo: also pause while the Esc menu is open |
| `General.PauseHotkey` | `F8` | Toggle the pause |
| `General.ShowNotifications` | `true` | PAUSED banner and paused/resumed messages |
| `Multiplayer.AllowMultiplayerPause` | `true` | Host only: let players pause the squad |

## Notes

- While paused outside the menu you can't move, look or shoot. Esc and chat still work.
- Press-and-hold menu buttons such as Retire to Lobby work while paused. Leaving the expedition resumes the game first.
- Menu sounds are silent during a pause, because all game audio is suspended.
