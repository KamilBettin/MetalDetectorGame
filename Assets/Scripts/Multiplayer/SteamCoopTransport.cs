using System;
using System.Text;
using UnityEngine;

#if STEAMWORKS_NET
using Steamworks;
#endif

public class SteamCoopTransport : MonoBehaviour
{
    private const int Channel = 0;

    public ulong LocalSteamId { get; private set; }
    public string StatusText { get; private set; } = "Steam transport not started.";

    private LocalCoopManager coopManager;
    private ulong hostSteamId;

#if STEAMWORKS_NET
    private Callback<P2PSessionRequest_t> sessionRequestCallback;
    private bool isStarted;

    public bool StartTransport(LocalCoopManager manager)
    {
        coopManager = manager;

        try
        {
            if (!SteamAPI.IsSteamRunning())
            {
                StatusText = "Steam client is not running.";
                return false;
            }

            if (!SteamAPI.Init())
            {
                StatusText = "SteamAPI.Init failed. Check steam_appid.txt/AppID.";
                return false;
            }

            sessionRequestCallback = Callback<P2PSessionRequest_t>.Create(OnP2PSessionRequest);
            LocalSteamId = SteamUser.GetSteamID().m_SteamID;
            StatusText = "Steam ready as " + SteamFriends.GetPersonaName() + " (" + LocalSteamId + ")";
            isStarted = true;
            return true;
        }
        catch (Exception exception)
        {
            StatusText = "Steam failed: " + exception.Message;
            return false;
        }
    }

    public void StopTransport()
    {
        isStarted = false;
        hostSteamId = 0;
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
        SteamNetworking.SendP2PPacket(new CSteamID(steamId), payload, (uint)payload.Length, EP2PSend.k_EP2PSendReliable, Channel);
    }

    public void Tick()
    {
        if (!isStarted)
        {
            return;
        }

        SteamAPI.RunCallbacks();
        PollPackets();
    }

    private void PollPackets()
    {
        while (SteamNetworking.IsP2PPacketAvailable(out uint packetSize, Channel))
        {
            byte[] payload = new byte[packetSize];

            if (!SteamNetworking.ReadP2PPacket(payload, packetSize, out uint bytesRead, out CSteamID sender, Channel))
            {
                continue;
            }

            string line = Encoding.UTF8.GetString(payload, 0, (int)bytesRead);
            coopManager.ReceiveSteamLine(sender.m_SteamID, line);
        }
    }

    private void OnP2PSessionRequest(P2PSessionRequest_t request)
    {
        SteamNetworking.AcceptP2PSessionWithUser(request.m_steamIDRemote);
    }
#else
    public bool StartTransport(LocalCoopManager manager)
    {
        coopManager = manager;
        StatusText = "Steamworks.NET is not installed/enabled. Add Steamworks.NET and define STEAMWORKS_NET.";
        return false;
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
