# GTFOPause

A BepInEx IL2CPP plugin that pauses GTFO the way Risk of Rain 2 does. User-facing docs are in [`thunderstore/README.md`](thunderstore/README.md).

## How it works

- **Freeze:** `Time.timeScale = 0` plus `AkSoundEngine.Suspend(false)`, the same approach as RoR2's `PauseScreenController`.
  - GTFO's `Clock` accumulates `Time.deltaTime`, so gameplay timers stop with it.
  - Outside the engine, only SNet networking, menu UI and the frame limiter read unscaled or real time, so menus and connections keep working. This comes from the Il2CppInterop xref cache.
  - The exception is press-and-hold menu buttons (`CM_TimedButton`): they fill and drain their hold timer by `Clock.Delta` (static offset `0x38`, set from `Time.deltaTime` in `Clock.Update`), found by disassembling `GameAssembly.dll`.
- **Solo** (`PauseLogic`): the hotkey adds a pause reason, and so does the Esc menu (`eFocusState.MainMenu`) when `PauseOnMenu` is on (off by default).
  - Any game-state change, checkpoint reload, popup, join/leave, host change or leaving the session force-resumes.
- **Multiplayer** (`Net/PauseNet.cs`): uses GTFO-API `NetworkAPI` events, following RoR2's `PauseStopController`.
  - Clients say hello to the host, and players ask the host to pause.
  - The host broadcasts the squad state and re-sends it every 2 s; clients resume on their own after 6 s without word from the host.
- **Patches:**
  - `InputMapper.DoGet*` reads as idle in FPS while paused.
  - `GS_InLevel.Update` pins `Clock.ExpeditionProgressionTime`.
  - `CM_TimedButton.OnHoverUpdate` / `Update` run with `Clock.Delta` set to the real frame time while paused, so holds like Retire to Lobby finish.
  - `SNet_SyncManager.SessionCommand` (Retire to Lobby sends one) and `SNet_SessionHub.LeaveHub` resume first, so leaving never runs at timeScale 0.

GTFO also ships a dormant `PauseManager` (it sets `timeScale` and broadcasts `Global.OnPaused`, but nothing in vanilla calls it). This plugin leaves it alone.

## Building (macOS or Windows)

1. Copy `GTFOPause/GameFolder.props.example` to `GTFOPause/GameFolder.props`.
2. Set `GameFolder` to an r2modman GTFO profile that has launched once, so `BepInEx/interop` exists.
3. Optionally, set `DeployFolder` to a test profile. Each build then copies the DLL into `BepInEx/plugins/GTFOPause/` there.
4. Build with `dotnet build -c Debug`.
5. Package with `./scripts/package.sh`. The script checks that the manifest, `Plugin.cs` and csproj versions match, then writes two zips:
   - `dist/GTFOPause-<version>.zip`: the Thunderstore package. It also works with r2modman's "Import local mod".
   - `dist/GTFOPause-<version>-manual.zip`: holds only `BepInEx/plugins/GTFOPause/GTFOPause.dll`. Extract it into a profile folder (r2modman: Settings → Browse profile folder).

`scripts/make-icon.py` regenerates `thunderstore/icon.png` (standard library only).

## In-game test checklist

These haven't been verified in game yet. Watch `BepInEx/LogOutput.log` for `Paused (...)`, `Resumed` and `Force resume: ...`.

**Solo with bots.** Pause for at least 60 s for each item:
- **Menu:** the Esc menu opens, animates and navigates while paused, and settings apply.
- **World:** sleeping and awake enemies and scouts stay frozen.
- **Bots:** they don't act; watch for a single shot.
- **Timers:** reactor countdown, alarm and blood-door waves, and warden "in N s" timers all stop.
- **Expedition timer:** the results screen leaves out paused time.
- **Tools and effects:** sentries, mines, C-foam and infection stay frozen.
- **Audio:** it freezes and resumes, with no burst of queued menu clicks on resume.
- **F8 pause:** in FPS it blocks move, look and shoot; Esc still opens the menu and the game stays paused.
- **Banner:** the PAUSED banner shows over interaction prompts, and also when no other message is up (no stuck white box from the message's intro).
- **Leave expedition:** works from the paused menu.
- **Hold buttons:** pause with F8, open Esc, and hold Retire to Lobby. The bar fills, drains when released, and retiring works. The log shows `Force resume: session command ...`.
- **PauseOnMenu:** off, Esc alone doesn't pause; on, it does, and the hold buttons still work.
- **F8 binding:** F8 isn't bound to anything in GTFO.

**Multiplayer.** Two machines, with the host on `AllowMultiplayerPause=true` (the default):
- **Pause and resume:** pause from either side, then resume from the other.
- **Long pause:** hold 3 minutes with no disconnect.
- **Setting off:** with `AllowMultiplayerPause=false`, F8 only shows "disabled by the host".
- **Missing mod:** a client without the mod makes the request fail with that player's name.
- **Host crash:** kill the host while paused; the client resumes after about 6 s.
- **Join or leave:** a player joining or leaving during a pause resumes the game.

If something keeps running while paused, these patches fit:
- **Bots:** prefix-skip `PlayerAIBot.UpdateActions`.
- **Reactor:** toggle `LG_WardenObjective_Reactor.m_progressUpdateEnabled` (Hikaria Core does this).
- **Broad leaks:** prefix-skip `GS_InLevel.Update` / `FixedUpdate` while paused.
