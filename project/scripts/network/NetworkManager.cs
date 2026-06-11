using Godot;
using System;

namespace Miao.Network;

public partial class NetworkManager : Node
{
    public static NetworkManager Instance { get; private set; }

    public bool IsHost { get; private set; }
    public bool IsConnected { get; private set; }
    public int LocalPlayerId { get; private set; }

    private ENetMultiplayerPeer _peer;

    [Signal]
    public delegate void PlayerConnectedEventHandler(int playerId);
    [Signal]
    public delegate void PlayerDisconnectedEventHandler(int playerId);
    [Signal]
    public delegate void ConnectionFailedEventHandler();
    [Signal]
    public delegate void ServerStartedEventHandler();

    public override void _Ready()
    {
        Instance = this;
        Multiplayer.PeerConnected += OnPeerConnected;
        Multiplayer.PeerDisconnected += OnPeerDisconnected;
        Multiplayer.ConnectedToServer += OnConnectedToServer;
        Multiplayer.ConnectionFailed += OnConnectionFailed;
    }

    public Error HostGame(int port = 7777, int maxPlayers = 4)
    {
        _peer = new ENetMultiplayerPeer();
        var error = _peer.CreateServer(port, maxPlayers);
        if (error != Error.Ok)
        {
            GD.PushError($"NetworkManager: Failed to create server: {error}");
            return error;
        }

        Multiplayer.MultiplayerPeer = _peer;
        IsHost = true;
        IsConnected = true;
        LocalPlayerId = 1;

        GD.Print($"Server started on port {port}");
        EmitSignal(SignalName.ServerStarted);
        return Error.Ok;
    }

    public Error JoinGame(string address, int port = 7777)
    {
        _peer = new ENetMultiplayerPeer();
        var error = _peer.CreateClient(address, port);
        if (error != Error.Ok)
        {
            GD.PushError($"NetworkManager: Failed to connect to server: {error}");
            return error;
        }

        Multiplayer.MultiplayerPeer = _peer;
        IsHost = false;

        GD.Print($"Connecting to {address}:{port}");
        return Error.Ok;
    }

    public void Disconnect()
    {
        if (_peer != null)
        {
            _peer.Close();
            _peer = null;
        }
        Multiplayer.MultiplayerPeer = null;
        IsHost = false;
        IsConnected = false;
        LocalPlayerId = 0;
    }

    private void OnPeerConnected(long id)
    {
        GD.Print($"Player connected: {id}");
        EmitSignal(SignalName.PlayerConnected, (int)id);
    }

    private void OnPeerDisconnected(long id)
    {
        GD.Print($"Player disconnected: {id}");
        EmitSignal(SignalName.PlayerDisconnected, (int)id);
    }

    private void OnConnectedToServer()
    {
        IsConnected = true;
        LocalPlayerId = Multiplayer.GetUniqueId();
        GD.Print($"Connected to server, local ID: {LocalPlayerId}");
    }

    private void OnConnectionFailed()
    {
        GD.PushError("Connection failed");
        EmitSignal(SignalName.ConnectionFailed);
    }

    public int[] GetConnectedPlayerIds()
    {
        return Multiplayer.GetPeers();
    }
}
