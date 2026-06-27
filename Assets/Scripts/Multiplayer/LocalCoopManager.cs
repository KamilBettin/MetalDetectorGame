using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class LocalCoopManager : MonoBehaviour
{
    private const string MessageSeparator = "|";
    private const string DefaultPlayerName = "Hunter";
    private const float RemoteLerpSpeed = 10f;

    public enum CoopRole
    {
        Offline,
        Host,
        Client
    }

    private class PeerConnection
    {
        public int id;
        public string playerName;
        public TcpClient client;
        public StreamReader reader;
        public StreamWriter writer;
        public Thread readThread;
        public readonly object writeLock = new object();
    }

    private struct QueuedMessage
    {
        public int senderId;
        public ulong steamSenderId;
        public string line;
    }

    private class RemotePlayer
    {
        public GameObject root;
        public Transform transform;
        public Transform visualRoot;
        public TextMesh nameText;
        public Vector3 targetPosition;
        public Quaternion targetRotation;
        public int characterIndex = -1;
    }

    public static LocalCoopManager Instance { get; private set; }

    public int defaultPort = 7777;
    public string defaultAddress = "127.0.0.1";
    public float sendRate = 12f;

    public CoopRole Role { get; private set; }
    public bool IsRunning => Role != CoopRole.Offline;
    public string StatusText { get; private set; } = "Offline";
    public int LocalPlayerId => localPlayerId;
    public int RemotePlayerCount => remotePlayers.Count;

    public void RequestImmediateStateSend()
    {
        nextSendTime = 0f;
    }

    private readonly object queueLock = new object();
    private readonly Queue<QueuedMessage> queuedMessages = new Queue<QueuedMessage>();
    private readonly Dictionary<int, PeerConnection> hostPeers = new Dictionary<int, PeerConnection>();
    private readonly Dictionary<ulong, int> steamPeerIds = new Dictionary<ulong, int>();
    private readonly Dictionary<int, ulong> peerSteamIds = new Dictionary<int, ulong>();
    private readonly Dictionary<int, RemotePlayer> remotePlayers = new Dictionary<int, RemotePlayer>();
    private readonly List<int> disconnectedPeerIds = new List<int>();

    private TcpListener listener;
    private Thread acceptThread;
    private TcpClient clientConnection;
    private StreamReader clientReader;
    private StreamWriter clientWriter;
    private Thread clientReadThread;
    private readonly object clientWriteLock = new object();
    private SteamCoopTransport steamTransport;
    private bool usingSteamTransport;

    private bool networkThreadsRunning;
    private int localPlayerId;
    private int nextPeerId = 2;
    private string localPlayerName = DefaultPlayerName;
    private Transform localPlayer;
    private float nextSendTime;
    private Material remoteBodyMaterial;
    private Material remoteDetectorMaterial;
    private bool applyingRemoteTeamState;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        GameEvents.TreasureFound += HandleLocalTreasureFound;
    }

    private void OnDestroy()
    {
        GameEvents.TreasureFound -= HandleLocalTreasureFound;
        StopSession();

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        steamTransport?.Tick();
        CacheLocalPlayer();
        ProcessQueuedMessages();
        UpdateRemotePlayers();
        SendLocalStateIfReady();
    }

    public bool StartHost(int port, string playerName = DefaultPlayerName)
    {
        StopSession();

        try
        {
            localPlayerName = CleanPlayerName(playerName);
            localPlayerId = 1;
            nextPeerId = 2;
            networkThreadsRunning = true;
            listener = new TcpListener(IPAddress.Any, Mathf.Clamp(port, 1, 65535));
            listener.Start();
            acceptThread = new Thread(AcceptClientsLoop) { IsBackground = true };
            acceptThread.Start();
            Role = CoopRole.Host;
            StatusText = "Hosting on port " + port;
            MoveLocalPlayerToNetworkSpawn();
            return true;
        }
        catch (Exception exception)
        {
            StatusText = "Host failed: " + exception.Message;
            StopSession();
            return false;
        }
    }

    public bool StartClient(string address, int port, string playerName = DefaultPlayerName)
    {
        StopSession();

        try
        {
            localPlayerName = CleanPlayerName(playerName);
            networkThreadsRunning = true;
            clientConnection = new TcpClient();
            clientConnection.NoDelay = true;
            IAsyncResult connectResult = clientConnection.BeginConnect(address, Mathf.Clamp(port, 1, 65535), null, null);

            if (!connectResult.AsyncWaitHandle.WaitOne(1800))
            {
                throw new TimeoutException("Connection timed out.");
            }

            clientConnection.EndConnect(connectResult);
            NetworkStream stream = clientConnection.GetStream();
            clientReader = new StreamReader(stream);
            clientWriter = new StreamWriter(stream) { AutoFlush = true };
            SendToServer("HELLO" + MessageSeparator + Escape(localPlayerName));
            clientReadThread = new Thread(ClientReadLoop) { IsBackground = true };
            clientReadThread.Start();
            Role = CoopRole.Client;
            StatusText = "Joining " + address + ":" + port;
            return true;
        }
        catch (Exception exception)
        {
            StatusText = "Join failed: " + exception.Message;
            StopSession();
            return false;
        }
    }

    public bool StartSteamHost(string playerName = DefaultPlayerName)
    {
        StopSession();
        steamTransport = EnsureSteamTransport();

        if (steamTransport == null || !steamTransport.StartTransport(this))
        {
            StatusText = steamTransport != null ? steamTransport.StatusText : "Steam transport is missing.";
            StopSteamOnly();
            return false;
        }

        localPlayerName = CleanPlayerName(playerName);
        localPlayerId = 1;
        nextPeerId = 2;
        usingSteamTransport = true;
        Role = CoopRole.Host;
        StatusText = "Steam host ready. Your Steam ID: " + steamTransport.LocalSteamId;
        MoveLocalPlayerToNetworkSpawn();
        return true;
    }

    public bool StartSteamClient(ulong hostSteamId, string playerName = DefaultPlayerName)
    {
        StopSession();
        steamTransport = EnsureSteamTransport();

        if (hostSteamId == 0)
        {
            StatusText = "Enter host Steam ID.";
            StopSteamOnly();
            return false;
        }

        if (steamTransport == null || !steamTransport.StartTransport(this))
        {
            StatusText = steamTransport != null ? steamTransport.StatusText : "Steam transport is missing.";
            StopSteamOnly();
            return false;
        }

        localPlayerName = CleanPlayerName(playerName);
        usingSteamTransport = true;
        Role = CoopRole.Client;
        steamTransport.SetHost(hostSteamId);
        steamTransport.SendToHost("HELLO" + MessageSeparator + Escape(localPlayerName));
        StatusText = "Connecting to Steam host " + hostSteamId;
        return true;
    }

    public void StopSession()
    {
        networkThreadsRunning = false;
        usingSteamTransport = false;
        steamTransport?.StopTransport();
        Role = CoopRole.Offline;
        localPlayerId = 0;
        nextSendTime = 0f;

        try
        {
            listener?.Stop();
        }
        catch
        {
        }

        listener = null;

        foreach (PeerConnection peer in hostPeers.Values)
        {
            ClosePeer(peer);
        }

        hostPeers.Clear();
        steamPeerIds.Clear();
        peerSteamIds.Clear();

        try
        {
            clientConnection?.Close();
        }
        catch
        {
        }

        clientConnection = null;
        clientReader = null;
        clientWriter = null;

        foreach (RemotePlayer remotePlayer in remotePlayers.Values)
        {
            if (remotePlayer.root != null)
            {
                Destroy(remotePlayer.root);
            }
        }

        remotePlayers.Clear();
        StatusText = "Offline";
    }

    public void ReceiveSteamLine(ulong senderSteamId, string line)
    {
        EnqueueSteamMessage(senderSteamId, line);
    }

    private void AcceptClientsLoop()
    {
        while (networkThreadsRunning)
        {
            try
            {
                TcpClient acceptedClient = listener.AcceptTcpClient();
                acceptedClient.NoDelay = true;
                NetworkStream stream = acceptedClient.GetStream();
                PeerConnection peer = new PeerConnection
                {
                    id = nextPeerId++,
                    client = acceptedClient,
                    reader = new StreamReader(stream),
                    writer = new StreamWriter(stream) { AutoFlush = true },
                    playerName = "Guest"
                };

                string hello = peer.reader.ReadLine();
                if (!string.IsNullOrEmpty(hello) && hello.StartsWith("HELLO" + MessageSeparator, StringComparison.Ordinal))
                {
                    string[] helloParts = SplitMessage(hello);
                    if (helloParts.Length > 1)
                    {
                        peer.playerName = CleanPlayerName(Unescape(helloParts[1]));
                    }
                }

                lock (hostPeers)
                {
                    hostPeers.Add(peer.id, peer);
                }

                SendToPeer(peer, "WELCOME" + MessageSeparator + peer.id.ToString(CultureInfo.InvariantCulture) + MessageSeparator + Escape(localPlayerName));
                SendToPeer(peer, "JOIN" + MessageSeparator + "1" + MessageSeparator + Escape(localPlayerName));

                lock (hostPeers)
                {
                    foreach (PeerConnection existingPeer in hostPeers.Values)
                    {
                        if (existingPeer.id == peer.id)
                        {
                            continue;
                        }

                        SendToPeer(peer, "JOIN" + MessageSeparator + existingPeer.id.ToString(CultureInfo.InvariantCulture) + MessageSeparator + Escape(existingPeer.playerName));
                    }
                }

                BroadcastFromHost("JOIN" + MessageSeparator + peer.id.ToString(CultureInfo.InvariantCulture) + MessageSeparator + Escape(peer.playerName), peer.id);
                EnqueueMessage(0, "JOIN" + MessageSeparator + peer.id.ToString(CultureInfo.InvariantCulture) + MessageSeparator + Escape(peer.playerName));
                SendToPeer(peer, BuildTeamStateMessage());
                SendTreasureSnapshotToPeer(peer);

                peer.readThread = new Thread(() => HostReadLoop(peer)) { IsBackground = true };
                peer.readThread.Start();
            }
            catch
            {
                if (networkThreadsRunning)
                {
                    EnqueueMessage(0, "STATUS" + MessageSeparator + "Accept failed");
                }
            }
        }
    }

    private void HostReadLoop(PeerConnection peer)
    {
        try
        {
            while (networkThreadsRunning && peer.client != null && peer.client.Connected)
            {
                string line = peer.reader.ReadLine();

                if (line == null)
                {
                    break;
                }

                EnqueueMessage(peer.id, line);
            }
        }
        catch
        {
        }

        EnqueueMessage(0, "DISCONNECT" + MessageSeparator + peer.id.ToString(CultureInfo.InvariantCulture));
    }

    private void ClientReadLoop()
    {
        try
        {
            while (networkThreadsRunning && clientConnection != null && clientConnection.Connected)
            {
                string line = clientReader.ReadLine();

                if (line == null)
                {
                    break;
                }

                EnqueueMessage(0, line);
            }
        }
        catch
        {
        }

        EnqueueMessage(0, "SERVER_DISCONNECT");
    }

    private void ProcessQueuedMessages()
    {
        while (true)
        {
            QueuedMessage queuedMessage;

            lock (queueLock)
            {
                if (queuedMessages.Count == 0)
                {
                    break;
                }

                queuedMessage = queuedMessages.Dequeue();
            }

            ProcessMessage(queuedMessage.senderId, queuedMessage.steamSenderId, queuedMessage.line);
        }
    }

    private void ProcessMessage(int senderId, ulong steamSenderId, string line)
    {
        if (string.IsNullOrEmpty(line))
        {
            return;
        }

        string[] parts = SplitMessage(line);

        if (parts.Length == 0)
        {
            return;
        }

        switch (parts[0])
        {
            case "HELLO":
                HandleSteamHello(steamSenderId, parts);
                break;
            case "WELCOME":
                HandleWelcome(parts);
                break;
            case "JOIN":
                HandleJoin(parts);
                break;
            case "LEAVE":
                HandleLeave(parts);
                break;
            case "STATE":
                HandleState(parts);

                if (Role == CoopRole.Host && senderId > 0)
                {
                    BroadcastFromHost(line, senderId);
                }
                else if (Role == CoopRole.Host && steamSenderId != 0 && steamPeerIds.TryGetValue(steamSenderId, out int steamStateSenderId))
                {
                    BroadcastFromHost(line, steamStateSenderId);
                }

                break;
            case "TREASURE":
                HandleRemoteTreasureFound(parts);

                if (Role == CoopRole.Host && senderId > 0)
                {
                    BroadcastFromHost(line, senderId);
                }
                else if (Role == CoopRole.Host && steamSenderId != 0 && steamPeerIds.TryGetValue(steamSenderId, out int steamTreasureSenderId))
                {
                    BroadcastFromHost(line, steamTreasureSenderId);
                }

                break;
            case "REVEAL":
                HandleRemoteTreasureRevealed(parts);
                RelayHostMessage(senderId, steamSenderId, line);
                break;
            case "AREA":
                HandleRemoteAreaUnlocked(parts);
                RelayHostMessage(senderId, steamSenderId, line);
                SendTeamStateFromCurrentRole();
                break;
            case "TEAM":
                HandleTeamState(parts);
                RelayHostMessage(senderId, steamSenderId, line);
                break;
            case "DISCONNECT":
                HandleHostPeerDisconnect(parts);
                break;
            case "SERVER_DISCONNECT":
                StatusText = "Disconnected from host";
                StopSession();
                break;
            case "STATUS":
                if (parts.Length > 1)
                {
                    StatusText = Unescape(parts[1]);
                }

                break;
        }
    }

    private void HandleSteamHello(ulong senderSteamId, string[] parts)
    {
        if (Role != CoopRole.Host || !usingSteamTransport || senderSteamId == 0)
        {
            return;
        }

        string playerName = parts.Length > 1 ? CleanPlayerName(Unescape(parts[1])) : "Steam Guest";

        if (!steamPeerIds.TryGetValue(senderSteamId, out int peerId))
        {
            peerId = nextPeerId++;
            steamPeerIds.Add(senderSteamId, peerId);
            peerSteamIds.Add(peerId, senderSteamId);
        }

        steamTransport.SendToPeer(senderSteamId, "WELCOME" + MessageSeparator + peerId.ToString(CultureInfo.InvariantCulture) + MessageSeparator + Escape(localPlayerName));
        steamTransport.SendToPeer(senderSteamId, "JOIN" + MessageSeparator + "1" + MessageSeparator + Escape(localPlayerName));

        foreach (KeyValuePair<int, ulong> existingPeer in peerSteamIds)
        {
            if (existingPeer.Key == peerId)
            {
                continue;
            }

            steamTransport.SendToPeer(senderSteamId, "JOIN" + MessageSeparator + existingPeer.Key.ToString(CultureInfo.InvariantCulture) + MessageSeparator + Escape("Steam Player " + existingPeer.Key));
        }

        BroadcastFromHost("JOIN" + MessageSeparator + peerId.ToString(CultureInfo.InvariantCulture) + MessageSeparator + Escape(playerName), peerId);
        EnqueueMessage(0, "JOIN" + MessageSeparator + peerId.ToString(CultureInfo.InvariantCulture) + MessageSeparator + Escape(playerName));
        steamTransport.SendToPeer(senderSteamId, BuildTeamStateMessage());
        SendTreasureSnapshotToSteamPeer(senderSteamId);
        StatusText = "Steam hosting: " + remotePlayers.Count + " connected";
    }

    private void HandleWelcome(string[] parts)
    {
        if (parts.Length < 2 || !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int assignedId))
        {
            return;
        }

        localPlayerId = assignedId;
        StatusText = "Connected as player " + localPlayerId;
        MoveLocalPlayerToNetworkSpawn();
    }

    private void HandleJoin(string[] parts)
    {
        if (parts.Length < 2 || !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int playerId))
        {
            return;
        }

        string playerName = parts.Length > 2 ? Unescape(parts[2]) : "Player " + playerId;

        if (playerId == localPlayerId)
        {
            return;
        }

        EnsureRemotePlayer(playerId, playerName);
        StatusText = Role == CoopRole.Host ? "Hosting: " + remotePlayers.Count + " connected" : "Connected: " + remotePlayers.Count + " nearby";
    }

    private void HandleLeave(string[] parts)
    {
        if (parts.Length < 2 || !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int playerId))
        {
            return;
        }

        RemoveRemotePlayer(playerId);
    }

    private void HandleState(string[] parts)
    {
        if (parts.Length < 9
            || !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int playerId)
            || playerId == localPlayerId)
        {
            return;
        }

        Vector3 position = new Vector3(ParseFloat(parts[2]), ParseFloat(parts[3]), ParseFloat(parts[4]));
        Quaternion rotation = new Quaternion(ParseFloat(parts[5]), ParseFloat(parts[6]), ParseFloat(parts[7]), ParseFloat(parts[8]));
        int characterIndex = parts.Length > 9 ? ParseInt(parts[9]) : 0;
        RemotePlayer remotePlayer = EnsureRemotePlayer(playerId, "Player " + playerId);
        remotePlayer.targetPosition = position;
        remotePlayer.targetRotation = rotation;
        ApplyRemoteCharacter(remotePlayer, characterIndex);
    }

    private void HandleRemoteTreasureFound(string[] parts)
    {
        if (parts.Length < 2)
        {
            return;
        }

        MarkTreasureFound(Unescape(parts[1]));
    }

    private void HandleRemoteTreasureRevealed(string[] parts)
    {
        if (parts.Length < 2)
        {
            return;
        }

        MarkTreasureRevealed(Unescape(parts[1]));
    }

    private void HandleRemoteAreaUnlocked(string[] parts)
    {
        if (parts.Length < 2)
        {
            return;
        }

        UnlockSearchArea(Unescape(parts[1]), false);
    }

    private void HandleTeamState(string[] parts)
    {
        if (parts.Length < 8)
        {
            return;
        }

        applyingRemoteTeamState = true;

        try
        {
            int money = ParseInt(parts[1]);
            int gridSize = ParseInt(parts[2]);
            float detectionRange = ParseFloat(parts[3]);
            int rangeUpgradeCost = ParseInt(parts[4]);
            int inventoryUpgradeCost = ParseInt(parts[5]);
            bool shovelUpgraded = ParseInt(parts[6]) != 0;
            string unlockedAreas = Unescape(parts[7]);

            PlayerInventory[] inventories = FindObjectsByType<PlayerInventory>();

            foreach (PlayerInventory inventory in inventories)
            {
                if (inventory == null)
                {
                    continue;
                }

                inventory.money = money;
                inventory.gridSize = Mathf.Clamp(gridSize, 1, inventory.maxGridSize);
            }

            MetalDetector[] detectors = FindObjectsByType<MetalDetector>();

            foreach (MetalDetector detector in detectors)
            {
                if (detector == null)
                {
                    continue;
                }

                detector.detectionRange = Mathf.Max(1f, detectionRange);
            }

            UpgradeShop[] shops = FindObjectsByType<UpgradeShop>();

            foreach (UpgradeShop shop in shops)
            {
                if (shop == null)
                {
                    continue;
                }

                shop.rangeUpgradeCost = Mathf.Max(0, rangeUpgradeCost);
                shop.inventoryUpgradeCost = Mathf.Max(0, inventoryUpgradeCost);
                shop.shovelUpgraded = shovelUpgraded;
            }

            if (!string.IsNullOrEmpty(unlockedAreas))
            {
                string[] areaIds = unlockedAreas.Split(';');

                foreach (string areaId in areaIds)
                {
                    if (!string.IsNullOrEmpty(areaId))
                    {
                        UnlockSearchArea(areaId, false);
                    }
                }
            }
        }
        finally
        {
            applyingRemoteTeamState = false;
        }
    }

    private void HandleHostPeerDisconnect(string[] parts)
    {
        if (Role != CoopRole.Host || parts.Length < 2 || !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int peerId))
        {
            return;
        }

        disconnectedPeerIds.Add(peerId);
        BroadcastFromHost("LEAVE" + MessageSeparator + peerId.ToString(CultureInfo.InvariantCulture), peerId);
        RemoveRemotePlayer(peerId);
        StatusText = "Hosting: " + remotePlayers.Count + " connected";
    }

    private void SendLocalStateIfReady()
    {
        if (Role == CoopRole.Offline || localPlayerId <= 0 || localPlayer == null || Time.unscaledTime < nextSendTime)
        {
            return;
        }

        nextSendTime = Time.unscaledTime + 1f / Mathf.Max(1f, sendRate);
        Vector3 position = localPlayer.position;
        Quaternion rotation = localPlayer.rotation;
        string message = string.Join(
            MessageSeparator,
            "STATE",
            localPlayerId.ToString(CultureInfo.InvariantCulture),
            FormatFloat(position.x),
            FormatFloat(position.y),
            FormatFloat(position.z),
            FormatFloat(rotation.x),
            FormatFloat(rotation.y),
            FormatFloat(rotation.z),
            FormatFloat(rotation.w),
            PlayerCharacterSelection.SelectedAvatarToken.ToString(CultureInfo.InvariantCulture));

        if (Role == CoopRole.Host)
        {
            BroadcastFromHost(message, 0);
        }
        else if (usingSteamTransport)
        {
            steamTransport.SendToHost(message);
        }
        else
        {
            SendToServer(message);
        }
    }

    public void ReportTreasureRevealed(DetectableTreasure treasure)
    {
        if (Role == CoopRole.Offline || applyingRemoteTeamState || treasure == null || string.IsNullOrEmpty(treasure.multiplayerId))
        {
            return;
        }

        string message = "REVEAL" + MessageSeparator + Escape(treasure.multiplayerId);

        if (Role == CoopRole.Host)
        {
            BroadcastFromHost(message, 0);
        }
        else if (usingSteamTransport)
        {
            steamTransport.SendToHost(message);
        }
        else
        {
            SendToServer(message);
        }
    }

    public void ReportAreaUnlocked(SearchArea area)
    {
        if (Role == CoopRole.Offline || applyingRemoteTeamState || area == null)
        {
            return;
        }

        string message = "AREA" + MessageSeparator + Escape(area.MultiplayerId);

        if (Role == CoopRole.Host)
        {
            BroadcastFromHost(message, 0);
            SendTeamStateFromCurrentRole();
        }
        else if (usingSteamTransport)
        {
            steamTransport.SendToHost(message);
            SendTeamStateFromCurrentRole();
        }
        else
        {
            SendToServer(message);
            SendTeamStateFromCurrentRole();
        }
    }

    public void ReportTeamStateChanged()
    {
        if (Role == CoopRole.Offline || applyingRemoteTeamState)
        {
            return;
        }

        SendTeamStateFromCurrentRole();
    }

    private void HandleLocalTreasureFound(DetectableTreasure treasure)
    {
        if (Role == CoopRole.Offline || treasure == null || string.IsNullOrEmpty(treasure.multiplayerId))
        {
            return;
        }

        string message = "TREASURE" + MessageSeparator + Escape(treasure.multiplayerId);

        if (Role == CoopRole.Host)
        {
            BroadcastFromHost(message, 0);
        }
        else if (usingSteamTransport)
        {
            steamTransport.SendToHost(message);
        }
        else
        {
            SendToServer(message);
        }
    }

    private void MarkTreasureFound(string treasureId)
    {
        DetectableTreasure[] treasures = FindObjectsByType<DetectableTreasure>();

        foreach (DetectableTreasure treasure in treasures)
        {
            if (treasure == null || treasure.multiplayerId != treasureId || treasure.isFound)
            {
                continue;
            }

            treasure.isFound = true;

            if (treasure.revealMarker != null)
            {
                Destroy(treasure.revealMarker.gameObject);
            }

            Renderer treasureRenderer = treasure.GetComponent<Renderer>();

            if (treasureRenderer != null)
            {
                treasureRenderer.enabled = false;
            }

            Collider treasureCollider = treasure.GetComponent<Collider>();

            if (treasureCollider != null)
            {
                treasureCollider.enabled = false;
            }

            break;
        }
    }

    private void MarkTreasureRevealed(string treasureId)
    {
        DetectableTreasure[] treasures = FindObjectsByType<DetectableTreasure>();

        foreach (DetectableTreasure treasure in treasures)
        {
            if (treasure == null || treasure.multiplayerId != treasureId || treasure.isFound || treasure.isRevealed)
            {
                continue;
            }

            SearchMarker marker = CreateSyncedRevealMarker(treasure);
            treasure.Reveal(marker);
            break;
        }
    }

    private SearchMarker CreateSyncedRevealMarker(DetectableTreasure treasure)
    {
        GameObject markerObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        markerObject.name = "Dig Target Marker";
        Vector3 position = treasure.transform.position;
        markerObject.transform.position = new Vector3(position.x, GetGroundY(position) + 0.055f, position.z);
        markerObject.transform.localScale = new Vector3(0.14f, 0.025f, 0.14f);

        Collider markerCollider = markerObject.GetComponent<Collider>();

        if (markerCollider != null)
        {
            markerCollider.enabled = false;
        }

        SearchMarker marker = markerObject.AddComponent<SearchMarker>();
        marker.markerType = SearchMarker.MarkerType.FoundTreasure;
        marker.treasureRarity = treasure.rarity;
        marker.pulseAmount = 0.08f;
        return marker;
    }

    private float GetGroundY(Vector3 worldPosition)
    {
        foreach (Terrain terrain in Terrain.activeTerrains)
        {
            if (terrain == null)
            {
                continue;
            }

            Vector3 terrainPosition = terrain.transform.position;
            Vector3 terrainSize = terrain.terrainData.size;
            bool inside = worldPosition.x >= terrainPosition.x
                && worldPosition.x <= terrainPosition.x + terrainSize.x
                && worldPosition.z >= terrainPosition.z
                && worldPosition.z <= terrainPosition.z + terrainSize.z;

            if (!inside)
            {
                continue;
            }

            float normalizedX = Mathf.InverseLerp(terrainPosition.x, terrainPosition.x + terrainSize.x, worldPosition.x);
            float normalizedZ = Mathf.InverseLerp(terrainPosition.z, terrainPosition.z + terrainSize.z, worldPosition.z);
            return terrainPosition.y + terrain.terrainData.GetInterpolatedHeight(normalizedX, normalizedZ);
        }

        return worldPosition.y;
    }

    private void CacheLocalPlayer()
    {
        if (localPlayer != null)
        {
            return;
        }

        FirstPersonController firstPersonController = FindAnyObjectByType<FirstPersonController>();

        if (firstPersonController != null)
        {
            localPlayer = firstPersonController.transform;
            return;
        }

        PlayerInventory inventory = FindAnyObjectByType<PlayerInventory>();
        localPlayer = inventory != null ? inventory.transform : null;
    }

    private void MoveLocalPlayerToNetworkSpawn()
    {
        CacheLocalPlayer();

        if (localPlayer == null || localPlayerId <= 0)
        {
            return;
        }

        Vector3 offset = new Vector3((localPlayerId - 1) * 1.7f, 0f, 0f);
        CharacterController characterController = localPlayer.GetComponent<CharacterController>();
        bool wasEnabled = characterController != null && characterController.enabled;

        if (wasEnabled)
        {
            characterController.enabled = false;
        }

        localPlayer.position += offset;

        if (wasEnabled)
        {
            characterController.enabled = true;
        }
    }

    private RemotePlayer EnsureRemotePlayer(int playerId, string playerName)
    {
        if (remotePlayers.TryGetValue(playerId, out RemotePlayer remotePlayer))
        {
            if (remotePlayer.nameText != null)
            {
                remotePlayer.nameText.text = playerName;
            }

            return remotePlayer;
        }

        remotePlayer = CreateRemotePlayer(playerId, playerName);
        remotePlayers.Add(playerId, remotePlayer);
        return remotePlayer;
    }

    private RemotePlayer CreateRemotePlayer(int playerId, string playerName)
    {
        GameObject root = new GameObject("Remote Player " + playerId);
        root.transform.position = localPlayer != null ? localPlayer.position + Vector3.right * playerId : Vector3.zero;

        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(root.transform, false);

        GameObject label = new GameObject("Name");
        label.transform.SetParent(root.transform, false);
        label.transform.localPosition = new Vector3(0f, 2.1f, 0f);
        TextMesh textMesh = label.AddComponent<TextMesh>();
        textMesh.text = playerName;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.characterSize = 0.22f;
        textMesh.fontSize = 48;
        textMesh.color = new Color(1f, 0.85f, 0.34f, 1f);

        RemotePlayer remotePlayer = new RemotePlayer
        {
            root = root,
            transform = root.transform,
            visualRoot = visual.transform,
            nameText = textMesh,
            targetPosition = root.transform.position,
            targetRotation = root.transform.rotation
        };

        ApplyRemoteCharacter(remotePlayer, 0);
        return remotePlayer;
    }

    private void ApplyRemoteCharacter(RemotePlayer remotePlayer, int characterIndex)
    {
        if (remotePlayer == null || remotePlayer.visualRoot == null)
        {
            return;
        }

        if (remotePlayer.characterIndex == characterIndex)
        {
            return;
        }

        remotePlayer.characterIndex = characterIndex;

        for (int i = remotePlayer.visualRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(remotePlayer.visualRoot.GetChild(i).gameObject);
        }

        PlayerCharacterSelection.CharacterProfile profile = PlayerCharacterSelection.GetProfile(characterIndex);

        if (UmaCharacterFactory.TryCreateCharacter(remotePlayer.visualRoot, profile, out _))
        {
            return;
        }

        BuildFallbackRemoteCharacter(remotePlayer.visualRoot, profile);
    }

    private void BuildFallbackRemoteCharacter(Transform parent, PlayerCharacterSelection.CharacterProfile profile)
    {
        Material bodyMaterial = CreateMaterial(profile.bodyColor);
        Material accentMaterial = CreateMaterial(profile.accentColor);
        Material skinMaterial = CreateMaterial(new Color(0.86f, 0.65f, 0.48f, 1f));

        CreateFallbackPart(parent, "Torso", PrimitiveType.Cube, new Vector3(0f, 1.08f, 0f), Quaternion.identity, new Vector3(0.42f, 0.74f, 0.24f), bodyMaterial);
        CreateFallbackPart(parent, "Head", PrimitiveType.Sphere, new Vector3(0f, 1.72f, 0f), Quaternion.identity, new Vector3(0.30f, 0.30f, 0.30f), skinMaterial);
        CreateFallbackPart(parent, "Hat Brim", PrimitiveType.Cylinder, new Vector3(0f, 1.90f, 0f), Quaternion.identity, new Vector3(0.34f, 0.035f, 0.34f), accentMaterial);
        CreateFallbackPart(parent, "Hat Crown", PrimitiveType.Cylinder, new Vector3(0f, 1.98f, 0f), Quaternion.identity, new Vector3(0.22f, 0.10f, 0.22f), accentMaterial);
        CreateFallbackPart(parent, "Left Arm", PrimitiveType.Cube, new Vector3(-0.34f, 1.04f, 0.02f), Quaternion.Euler(0f, 0f, -8f), new Vector3(0.14f, 0.62f, 0.14f), bodyMaterial);
        CreateFallbackPart(parent, "Right Arm", PrimitiveType.Cube, new Vector3(0.34f, 1.04f, 0.02f), Quaternion.Euler(0f, 0f, 8f), new Vector3(0.14f, 0.62f, 0.14f), bodyMaterial);
        CreateFallbackPart(parent, "Left Leg", PrimitiveType.Cube, new Vector3(-0.12f, 0.42f, 0f), Quaternion.identity, new Vector3(0.16f, 0.68f, 0.16f), bodyMaterial);
        CreateFallbackPart(parent, "Right Leg", PrimitiveType.Cube, new Vector3(0.12f, 0.42f, 0f), Quaternion.identity, new Vector3(0.16f, 0.68f, 0.16f), bodyMaterial);

        GameObject detectorHandle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        detectorHandle.name = "Detector Handle";
        detectorHandle.transform.SetParent(parent, false);
        detectorHandle.transform.localPosition = new Vector3(0.46f, 0.62f, 0.34f);
        detectorHandle.transform.localRotation = Quaternion.Euler(58f, 0f, 20f);
        detectorHandle.transform.localScale = new Vector3(0.035f, 0.55f, 0.035f);
        DisableCollider(detectorHandle);
        SetMaterial(detectorHandle, GetRemoteDetectorMaterial());

        GameObject detectorCoil = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        detectorCoil.name = "Detector Coil";
        detectorCoil.transform.SetParent(parent, false);
        detectorCoil.transform.localPosition = new Vector3(0.72f, 0.08f, 0.56f);
        detectorCoil.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        detectorCoil.transform.localScale = new Vector3(0.22f, 0.026f, 0.22f);
        DisableCollider(detectorCoil);
        SetMaterial(detectorCoil, GetRemoteDetectorMaterial());
    }

    private GameObject CreateFallbackPart(Transform parent, string partName, PrimitiveType primitiveType, Vector3 localPosition, Quaternion localRotation, Vector3 localScale, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(primitiveType);
        part.name = partName;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localRotation = localRotation;
        part.transform.localScale = localScale;
        DisableCollider(part);
        SetMaterial(part, material);
        return part;
    }

    private void UpdateRemotePlayers()
    {
        Camera mainCamera = Camera.main;

        foreach (RemotePlayer remotePlayer in remotePlayers.Values)
        {
            if (remotePlayer.root == null)
            {
                continue;
            }

            remotePlayer.transform.position = Vector3.Lerp(remotePlayer.transform.position, remotePlayer.targetPosition, Time.unscaledDeltaTime * RemoteLerpSpeed);
            remotePlayer.transform.rotation = Quaternion.Slerp(remotePlayer.transform.rotation, remotePlayer.targetRotation, Time.unscaledDeltaTime * RemoteLerpSpeed);

            if (remotePlayer.nameText != null && mainCamera != null)
            {
                remotePlayer.nameText.transform.rotation = Quaternion.LookRotation(remotePlayer.nameText.transform.position - mainCamera.transform.position, Vector3.up);
            }
        }
    }

    private void RemoveRemotePlayer(int playerId)
    {
        if (!remotePlayers.TryGetValue(playerId, out RemotePlayer remotePlayer))
        {
            return;
        }

        if (remotePlayer.root != null)
        {
            Destroy(remotePlayer.root);
        }

        remotePlayers.Remove(playerId);
    }

    private void RelayHostMessage(int senderId, ulong steamSenderId, string line)
    {
        if (Role != CoopRole.Host)
        {
            return;
        }

        if (senderId > 0)
        {
            BroadcastFromHost(line, senderId);
            return;
        }

        if (steamSenderId != 0 && steamPeerIds.TryGetValue(steamSenderId, out int steamPeerId))
        {
            BroadcastFromHost(line, steamPeerId);
        }
    }

    private void SendTeamStateFromCurrentRole()
    {
        string message = BuildTeamStateMessage();

        if (Role == CoopRole.Host)
        {
            BroadcastFromHost(message, 0);
            return;
        }

        if (usingSteamTransport)
        {
            steamTransport.SendToHost(message);
            return;
        }

        SendToServer(message);
    }

    private string BuildTeamStateMessage()
    {
        PlayerInventory inventory = FindAnyObjectByType<PlayerInventory>();
        MetalDetector detector = FindAnyObjectByType<MetalDetector>();
        UpgradeShop shop = FindAnyObjectByType<UpgradeShop>();
        string unlockedAreas = BuildUnlockedAreasList();

        int money = inventory != null ? inventory.money : 0;
        int gridSize = inventory != null ? inventory.gridSize : 3;
        float detectionRange = detector != null ? detector.detectionRange : 6f;
        int rangeUpgradeCost = shop != null ? shop.rangeUpgradeCost : 75;
        int inventoryUpgradeCost = shop != null ? shop.inventoryUpgradeCost : 120;
        int shovelUpgraded = shop != null && shop.shovelUpgraded ? 1 : 0;

        return string.Join(
            MessageSeparator,
            "TEAM",
            money.ToString(CultureInfo.InvariantCulture),
            gridSize.ToString(CultureInfo.InvariantCulture),
            FormatFloat(detectionRange),
            rangeUpgradeCost.ToString(CultureInfo.InvariantCulture),
            inventoryUpgradeCost.ToString(CultureInfo.InvariantCulture),
            shovelUpgraded.ToString(CultureInfo.InvariantCulture),
            Escape(unlockedAreas));
    }

    private string BuildUnlockedAreasList()
    {
        SearchArea[] areas = FindObjectsByType<SearchArea>();
        List<string> unlockedAreaIds = new List<string>();

        foreach (SearchArea area in areas)
        {
            if (area != null && area.isUnlocked)
            {
                unlockedAreaIds.Add(area.MultiplayerId);
            }
        }

        unlockedAreaIds.Sort(StringComparer.Ordinal);
        return string.Join(";", unlockedAreaIds);
    }

    private void UnlockSearchArea(string areaId, bool notifyMultiplayer)
    {
        SearchArea[] areas = FindObjectsByType<SearchArea>();

        foreach (SearchArea area in areas)
        {
            if (area == null || area.MultiplayerId != areaId)
            {
                continue;
            }

            area.SetUnlocked(true, notifyMultiplayer);
            break;
        }
    }

    private void SendTreasureSnapshotToPeer(PeerConnection peer)
    {
        DetectableTreasure[] treasures = FindObjectsByType<DetectableTreasure>();

        foreach (DetectableTreasure treasure in treasures)
        {
            if (treasure == null || string.IsNullOrEmpty(treasure.multiplayerId))
            {
                continue;
            }

            if (treasure.isFound)
            {
                SendToPeer(peer, "TREASURE" + MessageSeparator + Escape(treasure.multiplayerId));
            }
            else if (treasure.isRevealed)
            {
                SendToPeer(peer, "REVEAL" + MessageSeparator + Escape(treasure.multiplayerId));
            }
        }
    }

    private void SendTreasureSnapshotToSteamPeer(ulong steamId)
    {
        DetectableTreasure[] treasures = FindObjectsByType<DetectableTreasure>();

        foreach (DetectableTreasure treasure in treasures)
        {
            if (treasure == null || string.IsNullOrEmpty(treasure.multiplayerId))
            {
                continue;
            }

            if (treasure.isFound)
            {
                steamTransport.SendToPeer(steamId, "TREASURE" + MessageSeparator + Escape(treasure.multiplayerId));
            }
            else if (treasure.isRevealed)
            {
                steamTransport.SendToPeer(steamId, "REVEAL" + MessageSeparator + Escape(treasure.multiplayerId));
            }
        }
    }

    private void BroadcastFromHost(string line, int exceptPeerId)
    {
        if (Role != CoopRole.Host)
        {
            return;
        }

        if (usingSteamTransport)
        {
            foreach (KeyValuePair<int, ulong> peer in peerSteamIds)
            {
                if (peer.Key == exceptPeerId)
                {
                    continue;
                }

                steamTransport.SendToPeer(peer.Value, line);
            }

            return;
        }

        lock (hostPeers)
        {
            foreach (PeerConnection peer in hostPeers.Values)
            {
                if (peer.id == exceptPeerId)
                {
                    continue;
                }

                SendToPeer(peer, line);
            }
        }
    }

    private void SendToPeer(PeerConnection peer, string line)
    {
        try
        {
            lock (peer.writeLock)
            {
                peer.writer.WriteLine(line);
            }
        }
        catch
        {
            EnqueueMessage(0, "DISCONNECT" + MessageSeparator + peer.id.ToString(CultureInfo.InvariantCulture));
        }
    }

    private void SendToServer(string line)
    {
        if (clientWriter == null)
        {
            return;
        }

        try
        {
            lock (clientWriteLock)
            {
                clientWriter.WriteLine(line);
            }
        }
        catch
        {
            EnqueueMessage(0, "SERVER_DISCONNECT");
        }
    }

    private void EnqueueMessage(int senderId, string line)
    {
        lock (queueLock)
        {
            queuedMessages.Enqueue(new QueuedMessage { senderId = senderId, steamSenderId = 0, line = line });
        }
    }

    private void EnqueueSteamMessage(ulong senderSteamId, string line)
    {
        lock (queueLock)
        {
            queuedMessages.Enqueue(new QueuedMessage { senderId = 0, steamSenderId = senderSteamId, line = line });
        }
    }

    private void ClosePeer(PeerConnection peer)
    {
        try
        {
            peer.client?.Close();
        }
        catch
        {
        }
    }

    private void LateUpdate()
    {
        if (disconnectedPeerIds.Count == 0)
        {
            return;
        }

        lock (hostPeers)
        {
            foreach (int peerId in disconnectedPeerIds)
            {
                if (hostPeers.TryGetValue(peerId, out PeerConnection peer))
                {
                    ClosePeer(peer);
                    hostPeers.Remove(peerId);
                }
            }
        }

        disconnectedPeerIds.Clear();
    }

    private Material GetRemoteBodyMaterial()
    {
        if (remoteBodyMaterial == null)
        {
            remoteBodyMaterial = CreateMaterial(new Color(0.17f, 0.45f, 0.55f, 1f));
        }

        return remoteBodyMaterial;
    }

    private Material GetRemoteDetectorMaterial()
    {
        if (remoteDetectorMaterial == null)
        {
            remoteDetectorMaterial = CreateMaterial(new Color(0.08f, 0.065f, 0.045f, 1f));
        }

        return remoteDetectorMaterial;
    }

    private Material CreateMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");

        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        Material material = new Material(shader)
        {
            color = color
        };

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        return material;
    }

    private void SetMaterial(GameObject target, Material material)
    {
        Renderer targetRenderer = target.GetComponent<Renderer>();

        if (targetRenderer != null)
        {
            targetRenderer.material = material;
        }
    }

    private void DisableCollider(GameObject target)
    {
        Collider targetCollider = target.GetComponent<Collider>();

        if (targetCollider != null)
        {
            targetCollider.enabled = false;
        }
    }

    private void DisableCollidersInChildren(GameObject target)
    {
        Collider[] colliders = target.GetComponentsInChildren<Collider>();

        foreach (Collider targetCollider in colliders)
        {
            if (targetCollider != null)
            {
                targetCollider.enabled = false;
            }
        }
    }

    private string[] SplitMessage(string line)
    {
        return line.Split(new[] { MessageSeparator }, StringSplitOptions.None);
    }

    private string Escape(string value)
    {
        return string.IsNullOrEmpty(value)
            ? ""
            : value.Replace("%", "%25").Replace("|", "%7C").Replace("\n", "").Replace("\r", "");
    }

    private string Unescape(string value)
    {
        return string.IsNullOrEmpty(value)
            ? ""
            : value.Replace("%7C", "|").Replace("%25", "%");
    }

    private string CleanPlayerName(string playerName)
    {
        if (string.IsNullOrWhiteSpace(playerName))
        {
            return DefaultPlayerName;
        }

        playerName = playerName.Trim();
        return playerName.Length > 16 ? playerName.Substring(0, 16) : playerName;
    }

    private string FormatFloat(float value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private SteamCoopTransport EnsureSteamTransport()
    {
        if (steamTransport != null)
        {
            return steamTransport;
        }

        steamTransport = GetComponent<SteamCoopTransport>();

        if (steamTransport == null)
        {
            steamTransport = gameObject.AddComponent<SteamCoopTransport>();
        }

        return steamTransport;
    }

    private void StopSteamOnly()
    {
        usingSteamTransport = false;

        if (steamTransport != null)
        {
            steamTransport.StopTransport();
        }
    }

    private float ParseFloat(string value)
    {
        return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float result) ? result : 0f;
    }

    private int ParseInt(string value)
    {
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result) ? result : 0;
    }

    private void OnGUI()
    {
        if (Role == CoopRole.Offline)
        {
            return;
        }

        Rect panelRect = new Rect(20f, Screen.height - 126f, 360f, 48f);
        GameGui.DrawPanel(panelRect, "");
        GUI.Label(
            new Rect(panelRect.x + 12f, panelRect.y + 8f, panelRect.width - 24f, 30f),
            Role + " | " + StatusText + " | Players: " + (remotePlayers.Count + 1),
            GameGui.SmallLabelStyle);
    }
}
