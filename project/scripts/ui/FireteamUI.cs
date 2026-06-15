using Godot;
using System.Collections.Generic;
using Miao.Network;

namespace Miao.UI;

public partial class FireteamUI : CanvasLayer
{
    private Label _statusLabel;
    private VBoxContainer _playerList;
    private Button _hostBtn;
    private Button _joinBtn;
    private Button _disconnectBtn;
    private LineEdit _ipInput;
    private Button _readyBtn;
    private Label _readyLabel;

    private bool _isReady = false;
    private Dictionary<int, bool> _remoteReadyStates = new();

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;

        var panel = UIStyle.Panel(new Vector2(500, 400));
        panel.Position = new Vector2(390, 160);

        var vbox = new VBoxContainer();

        // Title
        var title = UIStyle.MakeLabel("火力战队通讯仪", 20, null, true);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(title);

        vbox.AddChild(new HSeparator());

        // Status
        _statusLabel = UIStyle.MakeLabel("未连接", 14, UIStyle.TextSecondary);
        vbox.AddChild(_statusLabel);

        // Player list
        _playerList = new VBoxContainer();
        vbox.AddChild(_playerList);

        vbox.AddChild(new HSeparator());

        // Host controls
        var hostBox = new HBoxContainer();
        _hostBtn = UIStyle.MakeButton("创建主机", new Vector2(120, 36));
        _hostBtn.Pressed += OnHostClicked;
        hostBox.AddChild(_hostBtn);

        _disconnectBtn = UIStyle.MakeButton("断开连接", new Vector2(120, 36));
        _disconnectBtn.Pressed += OnDisconnectClicked;
        _disconnectBtn.Disabled = true;
        hostBox.AddChild(_disconnectBtn);
        vbox.AddChild(hostBox);

        // Join controls
        var joinBox = new HBoxContainer();
        _ipInput = new LineEdit();
        _ipInput.PlaceholderText = "输入主机IP地址";
        _ipInput.Text = "127.0.0.1";
        _ipInput.CustomMinimumSize = new Vector2(200, 0);
        joinBox.AddChild(_ipInput);

        _joinBtn = UIStyle.MakeButton("加入游戏", new Vector2(120, 36));
        _joinBtn.Pressed += OnJoinClicked;
        joinBox.AddChild(_joinBtn);
        vbox.AddChild(joinBox);

        vbox.AddChild(new HSeparator());

        // Ready
        var readyBox = new HBoxContainer();
        _readyBtn = UIStyle.MakeButton("准备", new Vector2(120, 36));
        _readyBtn.Pressed += OnReadyClicked;
        _readyBtn.Disabled = true;
        readyBox.AddChild(_readyBtn);

        _readyLabel = UIStyle.MakeLabel("", 14, UIStyle.AccentGreen);
        readyBox.AddChild(_readyLabel);
        vbox.AddChild(readyBox);

        // Close
        var closeBtn = UIStyle.MakeButton("关闭", new Vector2(120, 36));
        closeBtn.Pressed += () => Hide();
        vbox.AddChild(closeBtn);

        panel.AddChild(vbox);
        AddChild(panel);

        // Connect signals
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.PlayerConnected += OnPlayerConnected;
            NetworkManager.Instance.PlayerDisconnected += OnPlayerDisconnected;
            NetworkManager.Instance.ConnectionFailed += OnConnectionFailed;
        }

        Hide();
    }

    public void Open()
    {
        RefreshUI();
        Show();
    }

    private void RefreshUI()
    {
        var net = NetworkManager.Instance;
        if (net == null) return;

        if (net.IsConnected)
        {
            _statusLabel.Text = net.IsHost ? "主机模式 - 等待玩家加入" : $"已连接 - ID: {net.LocalPlayerId}";
            _hostBtn.Disabled = true;
            _joinBtn.Disabled = true;
            _disconnectBtn.Disabled = false;
            _readyBtn.Disabled = false;
        }
        else
        {
            _statusLabel.Text = "未连接";
            _hostBtn.Disabled = false;
            _joinBtn.Disabled = false;
            _disconnectBtn.Disabled = true;
            _readyBtn.Disabled = true;
        }

        RefreshPlayerList();
    }

    private void RefreshPlayerList()
    {
        foreach (var child in _playerList.GetChildren()) child.QueueFree();

        var net = NetworkManager.Instance;
        if (net == null || !net.IsConnected) return;

        // Show local player
        var localLabel = UIStyle.MakeLabel(
            $"玩家 {net.LocalPlayerId} (本地) {(_isReady ? "[已准备]" : "")}",
            14, _isReady ? UIStyle.AccentGreen : UIStyle.TextPrimary);
        _playerList.AddChild(localLabel);

        // Show remote players
        foreach (var id in net.GetConnectedPlayerIds())
        {
            bool remoteReady = _remoteReadyStates.GetValueOrDefault(id, false);
            var label = UIStyle.MakeLabel(
                $"玩家 {id} {(remoteReady ? "[已准备]" : "")}",
                14, remoteReady ? UIStyle.AccentGreen : UIStyle.TextPrimary);
            _playerList.AddChild(label);
        }
    }

    private void OnHostClicked()
    {
        NetworkManager.Instance.HostGame();
        RefreshUI();
    }

    private void OnJoinClicked()
    {
        string ip = _ipInput.Text;
        if (string.IsNullOrEmpty(ip)) return;

        NetworkManager.Instance.JoinGame(ip);
        RefreshUI();
    }

    private void OnDisconnectClicked()
    {
        NetworkManager.Instance.Disconnect();
        _isReady = false;
        _readyBtn.Text = "准备";
        _readyLabel.Text = "";
        _remoteReadyStates.Clear();
        RefreshUI();
    }

    private void OnReadyClicked()
    {
        _isReady = !_isReady;
        _readyBtn.Text = _isReady ? "取消准备" : "准备";
        _readyLabel.Text = _isReady ? "已准备" : "";

        // 广播准备状态给其他玩家
        BroadcastReadyState(_isReady);
        RefreshPlayerList();
    }

    /// <summary>
    /// 广播准备状态给所有远程玩家
    /// </summary>
    private void BroadcastReadyState(bool isReady)
    {
        if (!Multiplayer.HasMultiplayerPeer()) return;

        int localId = (int)Multiplayer.GetUniqueId();
        Rpc(nameof(ReceiveReadyState), localId, isReady);
    }

    /// <summary>
    /// 接收远程玩家的准备状态
    /// </summary>
    [Rpc]
    public void ReceiveReadyState(int playerId, bool isReady)
    {
        // 忽略自己发给自己的广播
        if (playerId == (int)Multiplayer.GetUniqueId()) return;
        _remoteReadyStates[playerId] = isReady;
        RefreshPlayerList();
    }

    private void OnPlayerConnected(int playerId)
    {
        _remoteReadyStates[playerId] = false;
        RefreshUI();
    }

    private void OnPlayerDisconnected(int playerId)
    {
        _remoteReadyStates.Remove(playerId);
        RefreshUI();
    }

    private void OnConnectionFailed()
    {
        _statusLabel.Text = "连接失败";
        RefreshUI();
    }
}
