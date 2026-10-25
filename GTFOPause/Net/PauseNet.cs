using System.Collections.Generic;
using GTFO.API;
using SNetwork;
using UnityEngine;

namespace GTFOPause.Net;

/// <summary>
/// Multiplayer pause, modelled on Risk of Rain 2's PauseStopController: any player asks the host,
/// the host validates and broadcasts the squad state to everyone, and re-sends it every 2 s while
/// paused. Clients only trust the current master and resume on their own if the host goes quiet.
/// </summary>
internal static class PauseNet
{
    private const string HelloEvent = "GTFOPause.Hello";
    private const string RequestEvent = "GTFOPause.Request";
    private const string StateEvent = "GTFOPause.State";
    private const string DenyEvent = "GTFOPause.Deny";
    private const uint Protocol = 1;

    private const float ResendInterval = 2f;
    private const float WatchdogTimeout = 6f;
    private const float ToggleCooldown = 1f;

    private struct pHello
    {
        public uint Protocol;
    }

    private struct pRequest
    {
        public byte WantPaused;
    }

    private struct pState
    {
        public byte Paused;
        public ulong By;
        public uint Seq;
    }

    private struct pDeny
    {
        public byte Reason;
        public ulong Player;
    }

    private enum DenyReason : byte
    {
        Disabled = 1,
        MissingMod = 2,
        NotInLevel = 3,
        Cooldown = 4,
        ProtocolMismatch = 5,
    }

    // Host side
    private static readonly HashSet<ulong> s_moddedPlayers = new();
    private static readonly HashSet<ulong> s_lobbyScratch = new();
    private static bool s_squadPaused;
    private static ulong s_squadPausedBy;
    private static uint s_seq;
    private static float s_nextResend;
    private static float s_lastToggle = float.NegativeInfinity;

    // Client side
    private static ulong s_helloMaster;
    private static bool s_hostHasMod;
    private static ulong s_lastSender;
    private static uint s_lastSeq;
    private static uint? s_dismissedSeq;
    private static float s_lastStateTime;

    private static float Now => Time.realtimeSinceStartup;

    private static ulong LocalLookup => SNet.HasLocalPlayer ? SNet.LocalPlayer.Lookup : 0;

    public static void Register()
    {
        NetworkAPI.RegisterEvent<pHello>(HelloEvent, OnHello);
        NetworkAPI.RegisterEvent<pRequest>(RequestEvent, OnRequest);
        NetworkAPI.RegisterEvent<pState>(StateEvent, OnState);
        NetworkAPI.RegisterEvent<pDeny>(DenyEvent, OnDeny);
    }

    public static void Tick()
    {
        if (SNet.IsMaster)
            HostTick();
        else
            ClientTick();
    }

    /// <summary>The local player pressed the hotkey outside solo play.</summary>
    public static void OnHotkey()
    {
        if (SNet.IsMaster)
        {
            HostToggle(LocalLookup, !s_squadPaused);
            return;
        }

        if (!s_hostHasMod)
        {
            Notify.Event("The host doesn't have GTFOPause");
            return;
        }

        // Pressing the hotkey re-syncs a player who dismissed the current pause (see OnLocalForceResume).
        bool remotePaused = PauseState.Reasons.HasFlag(PauseReason.Remote);
        s_dismissedSeq = null;
        NetworkAPI.InvokeEvent(RequestEvent, new pRequest { WantPaused = (byte)(remotePaused ? 0 : 1) }, SNet.Master);
    }

    /// <summary>Called when the local game force-resumes (state change, join/leave, popup...).</summary>
    public static void OnLocalForceResume()
    {
        Notify.PausedBy = null;
        if (SNet.IsMaster)
        {
            if (s_squadPaused)
                SetSquadPaused(false, LocalLookup, applyLocally: false);
        }
        else if (PauseState.Reasons.HasFlag(PauseReason.Remote))
        {
            // Don't let the host's periodic re-send pause this player again until the next pause.
            s_dismissedSeq = s_lastSeq;
        }
    }

    // ---- Host ----

    private static void HostTick()
    {
        PruneModdedPlayers();

        if (s_squadPaused && Now >= s_nextResend)
            Broadcast();
    }

    private static void OnHello(ulong sender, pHello hello)
    {
        if (!SNet.IsMaster)
            return;

        if (hello.Protocol != Protocol)
        {
            Logger.Warn($"Player {sender} runs GTFOPause protocol {hello.Protocol}, expected {Protocol}");
            SendTo(sender, DenyEvent, new pDeny { Reason = (byte)DenyReason.ProtocolMismatch });
            return;
        }

        s_moddedPlayers.Add(sender);
        Logger.Info($"{NameOf(sender)} has GTFOPause");
        SendTo(sender, StateEvent, CurrentState());
    }

    private static void OnRequest(ulong sender, pRequest request)
    {
        if (SNet.IsMaster)
            HostToggle(sender, request.WantPaused != 0);
    }

    private static void HostToggle(ulong requester, bool wantPaused)
    {
        if (!PauseRules.InLevel)
        {
            Deny(requester, DenyReason.NotInLevel);
            return;
        }

        if (!PauseConfig.AllowMultiplayerPause.Value)
        {
            Deny(requester, DenyReason.Disabled);
            return;
        }

        if (wantPaused == s_squadPaused)
        {
            // Already there; bring a remote requester back in sync.
            if (requester != LocalLookup)
                SendTo(requester, StateEvent, CurrentState());
            return;
        }

        if (wantPaused && TryFindUnmoddedHuman(out ulong missing))
        {
            Deny(requester, DenyReason.MissingMod, missing);
            return;
        }

        if (Now - s_lastToggle < ToggleCooldown)
        {
            Deny(requester, DenyReason.Cooldown);
            return;
        }

        s_lastToggle = Now;
        SetSquadPaused(wantPaused, requester, applyLocally: true);
    }

    private static void SetSquadPaused(bool paused, ulong by, bool applyLocally)
    {
        s_squadPaused = paused;
        s_squadPausedBy = by;
        s_seq++;
        if (applyLocally)
            ApplyRemote(paused, by);
        Broadcast();
    }

    private static void Broadcast()
    {
        NetworkAPI.InvokeEvent(StateEvent, CurrentState());
        s_nextResend = Now + ResendInterval;
    }

    private static pState CurrentState() => new()
    {
        Paused = (byte)(s_squadPaused ? 1 : 0),
        By = s_squadPausedBy,
        Seq = s_seq,
    };

    private static bool TryFindUnmoddedHuman(out ulong lookup)
    {
        lookup = 0;
        ulong local = LocalLookup;
        var players = SNet.LobbyPlayers;
        if (players == null)
            return false;

        for (int i = 0; i < players.Count; i++)
        {
            var player = players[i];
            if (player == null || player.IsBot || player.Lookup == local)
                continue;
            if (!s_moddedPlayers.Contains(player.Lookup))
            {
                lookup = player.Lookup;
                return true;
            }
        }
        return false;
    }

    /// <summary>Forget players who left, so someone rejoining without the mod isn't trusted.</summary>
    private static void PruneModdedPlayers()
    {
        if (s_moddedPlayers.Count == 0)
            return;

        s_lobbyScratch.Clear();
        var players = SNet.LobbyPlayers;
        if (players != null)
        {
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i] != null)
                    s_lobbyScratch.Add(players[i].Lookup);
            }
        }
        s_moddedPlayers.RemoveWhere(lookup => !s_lobbyScratch.Contains(lookup));
    }

    private static void Deny(ulong requester, DenyReason reason, ulong player = 0)
    {
        var deny = new pDeny { Reason = (byte)reason, Player = player };
        if (requester == LocalLookup)
            OnDeny(requester, deny);
        else
            SendTo(requester, DenyEvent, deny);
    }

    // ---- Client ----

    private static void ClientTick()
    {
        if (!PauseRules.InLevel || !SNet.HasMaster || SNet.Master == null)
        {
            s_helloMaster = 0;
            return;
        }

        ulong master = SNet.Master.Lookup;
        if (master != s_helloMaster)
        {
            s_helloMaster = master;
            s_hostHasMod = false;
            NetworkAPI.InvokeEvent(HelloEvent, new pHello { Protocol = Protocol }, SNet.Master);
        }

        if (PauseState.Reasons.HasFlag(PauseReason.Remote) && Now - s_lastStateTime > WatchdogTimeout)
            PauseLogic.ForceResume("no word from the host");
    }

    private static void OnState(ulong sender, pState state)
    {
        if (SNet.IsMaster || !SNet.HasMaster || SNet.Master == null || sender != SNet.Master.Lookup)
            return;

        s_hostHasMod = true;
        s_lastStateTime = Now;

        if (sender == s_lastSender && state.Seq < s_lastSeq)
            return;
        s_lastSender = sender;
        s_lastSeq = state.Seq;

        bool paused = state.Paused != 0;
        if (paused && (state.Seq == s_dismissedSeq || !PauseRules.InLevel))
            return;
        if (paused == PauseState.Reasons.HasFlag(PauseReason.Remote))
            return;

        ApplyRemote(paused, state.By);
    }

    private static void OnDeny(ulong sender, pDeny deny)
    {
        switch ((DenyReason)deny.Reason)
        {
            case DenyReason.Disabled:
                Notify.Event("Multiplayer pause is disabled by the host");
                break;
            case DenyReason.MissingMod:
                Notify.Event($"{NameOf(deny.Player)} doesn't have GTFOPause");
                break;
            case DenyReason.ProtocolMismatch:
                Notify.Event("Your GTFOPause version doesn't match the host's");
                break;
            case DenyReason.NotInLevel:
            case DenyReason.Cooldown:
                break;
        }
    }

    // ---- Shared ----

    private static void ApplyRemote(bool paused, ulong by)
    {
        string name = NameOf(by);
        if (paused)
        {
            Notify.PausedBy = name;
            PauseState.Add(PauseReason.Remote);
            Notify.Event($"{name} paused the game");
        }
        else
        {
            Notify.PausedBy = null;
            PauseState.Remove(PauseReason.Remote);
            Notify.Event($"{name} resumed the game");
        }
    }

    private static void SendTo<T>(ulong lookup, string eventName, T payload) where T : struct
    {
        if (SNet.TryGetPlayer(lookup, out var player) && player != null)
            NetworkAPI.InvokeEvent(eventName, payload, player);
    }

    private static string NameOf(ulong lookup) =>
        SNet.TryGetPlayer(lookup, out var player) && player != null ? player.NickName : "Someone";
}
