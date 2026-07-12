using System;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

#if STEAMWORKS_NET
using Steamworks;
#endif

public class SteamCoopTransport : MonoBehaviour
{
    private const int Channel = 0;
    private const int MaxLobbyMembers = 4;

    public ulong LocalSteamId { get; private set; }
    public ulong CurrentLobbyId { get; private set; }
    public string LocalPersonaName { get; private set; } = "Steam Player";
    public string StatusText { get; private set; } = "Steam transport not started.";
    public bool HasLobby => CurrentLobbyId != 0;

    private LocalCoopManager coopManager;
    private ulong hostSteamId;

#if STEAMWORKS_NET
    private readonly IntPtr[] incomingMessages = new IntPtr[32];
    private Callback<SteamNetworkingMessagesSessionRequest_t> sessionRequestCallback;
    private Callback<SteamNetworkingMessagesSessionFailed_t> sessionFailedCallback;
    private Callback<GameLobbyJoinRequested_t> lobbyJoinRequestedCallback;
    private Callback<LobbyEnter_t> lobbyEnterCallback;
    private CallResult<LobbyCreated_t> lobbyCreatedCallResult;
    private bool isStarted;
    private bool isCreatingLobby;
    private bool checkedLaunchCommandLine;

    public bool StartTransport(LocalCoopManager manager)
    {
        coopManager = manager;

        if (isStarted)
        {
            return true;
        }

        try
        {
            if (!SteamAPI.IsSteamRunning())
            {
                StatusText = "Steam client is not running.";
                return false;
            }

            if (!SteamAPI.Init())
            {
                StatusText = "SteamAPI.Init failed. Check AppID and Steam account access.";
                return false;
            }

            isStarted = true;
            sessionRequestCallback = Callback<SteamNetworkingMessagesSessionRequest_t>.Create(OnSessionRequest);
            sessionFailedCallback = Callback<SteamNetworkingMessagesSessionFailed_t>.Create(OnSessionFailed);
            lobbyJoinRequestedCallback = Callback<GameLobbyJoinRequested_t>.Create(OnLobbyJoinRequested);
            lobbyEnterCallback = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
            lobbyCreatedCallResult = CallResult<LobbyCreated_t>.Create(OnLobbyCreated);

            SteamNetworkingUtils.InitRelayNetworkAccess();
            LocalSteamId = SteamUser.GetSteamID().m_SteamID;
            LocalPersonaName = SteamFriends.GetPersonaName();
            StatusText = "Steam ready as " + LocalPersonaName;
            TryJoinLobbyFromLaunchCommandLine();
            return true;
        }
        catch (Exception exception)
        {
            isStarted = false;
            StatusText = "Steam failed: " + exception.Message;
            return false;
        }
    }

    public bool StartFriendsOnlyLobby()
    {
        if (!isStarted)
        {
            StatusText = "Steam is not ready.";
            return false;
        }

        LeaveLobby();
        isCreatingLobby = true;
        SteamAPICall_t call = SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, MaxLobbyMembers);
        lobbyCreatedCallResult.Set(call);
        StatusText = "Creating friends-only Steam lobby...";
        return true;
    }

    public bool JoinLobby(ulong lobbyId)
    {
        if (!isStarted || lobbyId == 0)
        {
            StatusText = "Steam lobby ID is invalid.";
            return false;
        }

        LeaveLobby();
        SteamMatchmaking.JoinLobby(new CSteamID(lobbyId));
        StatusText = "Joining Steam lobby...";
        return true;
    }

    public bool OpenInviteOverlay()
    {
        if (!isStarted || CurrentLobbyId == 0)
        {
            StatusText = "Create or join a Steam lobby first.";
            return false;
        }

        SteamFriends.ActivateGameOverlayInviteDialog(new CSteamID(CurrentLobbyId));
        StatusText = "Steam friend invite overlay opened.";
        return true;
    }

    public void LeaveLobby()
    {
        if (!isStarted)
        {
            CurrentLobbyId = 0;
            hostSteamId = 0;
            return;
        }

        if (hostSteamId != 0)
        {
            SteamNetworkingIdentity hostIdentity = CreateIdentity(hostSteamId);
            SteamNetworkingMessages.CloseSessionWithUser(ref hostIdentity);
        }

        if (CurrentLobbyId != 0)
        {
            SteamMatchmaking.LeaveLobby(new CSteamID(CurrentLobbyId));
        }

        CurrentLobbyId = 0;
        hostSteamId = 0;
        isCreatingLobby = false;
    }

    public void StopTransport()
    {
        if (!isStarted)
        {
            return;
        }

        LeaveLobby();
        sessionRequestCallback?.Dispose();
        sessionFailedCallback?.Dispose();
        lobbyJoinRequestedCallback?.Dispose();
        lobbyEnterCallback?.Dispose();
        lobbyCreatedCallResult?.Dispose();
        sessionRequestCallback = null;
        sessionFailedCallback = null;
        lobbyJoinRequestedCallback = null;
        lobbyEnterCallback = null;
        lobbyCreatedCallResult = null;
        SteamAPI.Shutdown();
        isStarted = false;
        LocalSteamId = 0;
        StatusText = "Steam transport stopped.";
    }

    public void SetHost(ulong steamId)
    {
        hostSteamId = steamId;
    }

    public void SendToHost(string line)
    {
        if (hostSteamId != 0)
        {
            SendToPeer(hostSteamId, line);
        }
    }

    public void SendToPeer(ulong steamId, string line)
    {
        if (!isStarted || steamId == 0 || string.IsNullOrEmpty(line))
        {
            return;
        }

        byte[] payload = Encoding.UTF8.GetBytes(line);
        GCHandle pinnedPayload = GCHandle.Alloc(payload, GCHandleType.Pinned);

        try
        {
            SteamNetworkingIdentity identity = CreateIdentity(steamId);
            int sendFlags = Constants.k_nSteamNetworkingSend_Reliable
                | Constants.k_nSteamNetworkingSend_AutoRestartBrokenSession;
            EResult result = SteamNetworkingMessages.SendMessageToUser(
                ref identity,
                pinnedPayload.AddrOfPinnedObject(),
                (uint)payload.Length,
                sendFlags,
                Channel);

            if (result != EResult.k_EResultOK)
            {
                StatusText = "Steam send failed: " + result;
            }
        }
        finally
        {
            pinnedPayload.Free();
        }
    }

    public void Tick()
    {
        if (!isStarted)
        {
            return;
        }

        SteamAPI.RunCallbacks();
        PollMessages();
    }

    private void PollMessages()
    {
        int received;

        do
        {
            received = SteamNetworkingMessages.ReceiveMessagesOnChannel(Channel, incomingMessages, incomingMessages.Length);

            for (int i = 0; i < received; i++)
            {
                IntPtr pointer = incomingMessages[i];

                if (pointer == IntPtr.Zero)
                {
                    continue;
                }

                try
                {
                    SteamNetworkingMessage_t message = SteamNetworkingMessage_t.FromIntPtr(pointer);

                    if (message.m_cbSize <= 0 || message.m_pData == IntPtr.Zero)
                    {
                        continue;
                    }

                    byte[] payload = new byte[message.m_cbSize];
                    Marshal.Copy(message.m_pData, payload, 0, message.m_cbSize);
                    ulong senderSteamId = message.m_identityPeer.GetSteamID64();

                    if (senderSteamId != 0 && IsLobbyMember(senderSteamId))
                    {
                        coopManager?.ReceiveSteamLine(senderSteamId, Encoding.UTF8.GetString(payload));
                    }
                }
                finally
                {
                    SteamNetworkingMessage_t.Release(pointer);
                    incomingMessages[i] = IntPtr.Zero;
                }
            }
        }
        while (received == incomingMessages.Length);
    }

    private void OnLobbyCreated(LobbyCreated_t result, bool ioFailure)
    {
        isCreatingLobby = false;

        if (ioFailure || result.m_eResult != EResult.k_EResultOK || result.m_ulSteamIDLobby == 0)
        {
            StatusText = "Steam lobby creation failed: " + result.m_eResult;
            coopManager?.NotifySteamLobbyFailed(StatusText);
            return;
        }

        CurrentLobbyId = result.m_ulSteamIDLobby;
        CSteamID lobbyId = new CSteamID(CurrentLobbyId);
        SteamMatchmaking.SetLobbyData(lobbyId, "host", LocalSteamId.ToString());
        SteamMatchmaking.SetLobbyData(lobbyId, "version", Application.version);
        SteamMatchmaking.SetLobbyData(lobbyId, "maxPlayers", MaxLobbyMembers.ToString());
        StatusText = "Steam lobby ready (friends only, 4 players).";
        coopManager?.NotifySteamLobbyHosted(CurrentLobbyId);
        OpenInviteOverlay();
    }

    private void OnLobbyEntered(LobbyEnter_t result)
    {
        if ((EChatRoomEnterResponse)result.m_EChatRoomEnterResponse != EChatRoomEnterResponse.k_EChatRoomEnterResponseSuccess)
        {
            StatusText = "Could not enter Steam lobby: " + (EChatRoomEnterResponse)result.m_EChatRoomEnterResponse;
            coopManager?.NotifySteamLobbyFailed(StatusText);
            return;
        }

        CurrentLobbyId = result.m_ulSteamIDLobby;
        CSteamID lobbyId = new CSteamID(CurrentLobbyId);
        ulong ownerSteamId = SteamMatchmaking.GetLobbyOwner(lobbyId).m_SteamID;

        if (ownerSteamId == LocalSteamId)
        {
            StatusText = isCreatingLobby ? "Creating Steam lobby..." : "Steam lobby ready.";
            return;
        }

        if (ownerSteamId == 0)
        {
            StatusText = "Steam lobby has no host.";
            coopManager?.NotifySteamLobbyFailed(StatusText);
            return;
        }

        hostSteamId = ownerSteamId;
        StatusText = "Connected to Steam lobby. Contacting host...";
        coopManager?.ConnectToSteamLobbyHost(ownerSteamId, LocalPersonaName);
    }

    private void OnLobbyJoinRequested(GameLobbyJoinRequested_t request)
    {
        ulong lobbyId = request.m_steamIDLobby.m_SteamID;

        if (lobbyId == 0)
        {
            return;
        }

        coopManager?.PrepareForSteamLobbyJoin(LocalPersonaName);
        JoinLobby(lobbyId);
    }

    private void OnSessionRequest(SteamNetworkingMessagesSessionRequest_t request)
    {
        ulong remoteSteamId = request.m_identityRemote.GetSteamID64();

        if (remoteSteamId != 0 && IsLobbyMember(remoteSteamId))
        {
            SteamNetworkingIdentity identity = request.m_identityRemote;
            SteamNetworkingMessages.AcceptSessionWithUser(ref identity);
        }
    }

    private void OnSessionFailed(SteamNetworkingMessagesSessionFailed_t failure)
    {
        StatusText = "Steam connection interrupted: " + failure.m_info.m_eEndReason;
    }

    private bool IsLobbyMember(ulong steamId)
    {
        if (CurrentLobbyId == 0 || steamId == 0)
        {
            return false;
        }

        CSteamID lobbyId = new CSteamID(CurrentLobbyId);
        int memberCount = SteamMatchmaking.GetNumLobbyMembers(lobbyId);

        for (int i = 0; i < memberCount; i++)
        {
            if (SteamMatchmaking.GetLobbyMemberByIndex(lobbyId, i).m_SteamID == steamId)
            {
                return true;
            }
        }

        return false;
    }

    private void TryJoinLobbyFromLaunchCommandLine()
    {
        if (checkedLaunchCommandLine)
        {
            return;
        }

        checkedLaunchCommandLine = true;
        string commandLine = null;
        SteamApps.GetLaunchCommandLine(out commandLine, 2048);

        if (!TryParseLobbyId(commandLine, out ulong lobbyId))
        {
            commandLine = string.Join(" ", Environment.GetCommandLineArgs());
        }

        if (TryParseLobbyId(commandLine, out lobbyId))
        {
            coopManager?.PrepareForSteamLobbyJoin(LocalPersonaName);
            JoinLobby(lobbyId);
        }
    }

    private static bool TryParseLobbyId(string commandLine, out ulong lobbyId)
    {
        lobbyId = 0;

        if (string.IsNullOrWhiteSpace(commandLine))
        {
            return false;
        }

        const string marker = "+connect_lobby";
        int markerIndex = commandLine.IndexOf(marker, StringComparison.OrdinalIgnoreCase);

        if (markerIndex < 0)
        {
            return false;
        }

        string remainder = commandLine.Substring(markerIndex + marker.Length).TrimStart();

        if (string.IsNullOrEmpty(remainder))
        {
            return false;
        }

        string value = remainder.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)[0].Trim('"');
        return ulong.TryParse(value, out lobbyId) && lobbyId != 0;
    }

    private static SteamNetworkingIdentity CreateIdentity(ulong steamId)
    {
        SteamNetworkingIdentity identity = new SteamNetworkingIdentity();
        identity.SetSteamID64(steamId);
        return identity;
    }
#else
    public bool StartTransport(LocalCoopManager manager)
    {
        coopManager = manager;
        StatusText = "Steamworks.NET is not installed/enabled. Add Steamworks.NET and define STEAMWORKS_NET.";
        return false;
    }

    public bool StartFriendsOnlyLobby()
    {
        return false;
    }

    public bool JoinLobby(ulong lobbyId)
    {
        return false;
    }

    public bool OpenInviteOverlay()
    {
        return false;
    }

    public void LeaveLobby()
    {
        CurrentLobbyId = 0;
        hostSteamId = 0;
    }

    public void StopTransport()
    {
    }

    public void SetHost(ulong steamId)
    {
        hostSteamId = steamId;
    }

    public void SendToHost(string line)
    {
    }

    public void SendToPeer(ulong steamId, string line)
    {
    }

    public void Tick()
    {
    }
#endif
}
