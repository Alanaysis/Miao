using Godot;
using System.Collections.Generic;
using Miao.Pickup;
using Miao.UI;
using Miao.Weapon;

namespace Miao.System;

public partial class GameManager : Node
{
    public static GameManager Instance { get; private set; }

    public enum GameState { MainMenu, Playing, RoomTransition, Boss, Settlement, GameOver }
    public GameState CurrentState { get; private set; }

    /// <summary>当前选择的地图 ID（由 EquipmentScreen 设置）</summary>
    public string SelectedMapId { get; set; } = "nest";

    private Player.Player _player;
    private RoomGenerator _roomGenerator;
    private float _gameTime;
    private int _killCount;
    private int _glimmer;
    private EngramDecoder _engramDecoder;

    [Signal]
    public delegate void GlimmerChangedEventHandler(int amount);

    public override void _Ready()
    {
        if (Instance != null && Instance != this)
        {
            QueueFree();
            return;
        }
        Instance = this;
        InputConfig.Load();

        _engramDecoder = new EngramDecoder();
        AddChild(_engramDecoder);
    }

    public override void _ExitTree()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void StartGame()
    {
        CurrentState = GameState.Playing;
        _gameTime = 0;
        _killCount = 0;
        _glimmer = 0;
        _engramDecoder?.Clear();
        GetTree().ChangeSceneToFile("res://scenes/main.tscn");
    }

    public void RegisterPlayer(Player.Player player)
    {
        if (_player == player) return; // 防止重复注册
        _player = player;
        player.PlayerDied += OnPlayerDied;
    }

    public void RegisterRoomGenerator(RoomGenerator rg)
    {
        _roomGenerator = rg;
        rg.CurrentMapId = SelectedMapId;
        rg.RoomCleared += OnRoomCleared;
        rg.AllRoomsCleared += OnAllRoomsCleared;
        rg.StartRun(_player);
    }

    private CanvasLayer _roomClearUI;

    private void OnRoomCleared(int roomIndex)
    {
        CurrentState = GameState.RoomTransition;
        ShowRoomClearPrompt(roomIndex);
    }

    private void ShowRoomClearPrompt(int roomIndex)
    {
        if (_roomClearUI != null) _roomClearUI.QueueFree();

        _roomClearUI = new CanvasLayer();
        _roomClearUI.Layer = 10;
        _roomClearUI.ProcessMode = Node.ProcessModeEnum.Always;
        GetTree().CurrentScene.AddChild(_roomClearUI);

        // 放在武器面板上方，不重叠
        float panelW = 300, panelH = 100;
        float px = 960;
        float py = 530;

        var panel = UIStyle.Panel(new Vector2(panelW, panelH), UIStyle.AccentGreen);
        panel.Position = new Vector2(px, py);
        _roomClearUI.AddChild(panel);

        var title = UIStyle.MakeLabel($"✓ 房间 {roomIndex + 1} 已清空！", 16, UIStyle.AccentGreen, true);
        title.Position = new Vector2(12, 8);
        panel.AddChild(title);

        bool isLastRoom = roomIndex + 1 >= _roomGenerator.TotalRooms;
        string btnText = isLastRoom ? "结算" : "进入下一房间";

        var enterBtn = UIStyle.MakeButton(btnText, new Vector2(120, 36));
        enterBtn.Position = new Vector2(12, 44);
        enterBtn.ProcessMode = Node.ProcessModeEnum.Always;
        panel.AddChild(enterBtn);

        var waitBtn = UIStyle.MakeButton("稍后", new Vector2(100, 36));
        waitBtn.Position = new Vector2(140, 44);
        waitBtn.ProcessMode = Node.ProcessModeEnum.Always;
        panel.AddChild(waitBtn);

        enterBtn.Pressed += () => { HideRoomClearPrompt(); NextRoom(); };
        waitBtn.Pressed += () => HideRoomClearPrompt();
    }

    private void HideRoomClearPrompt()
    {
        if (_roomClearUI != null)
        {
            _roomClearUI.QueueFree();
            _roomClearUI = null;
        }
    }

    public void NextRoom()
    {
        HideRoomClearPrompt();
        CurrentState = GameState.Playing;
        _roomGenerator.AdvanceRoom();
    }

    private void OnAllRoomsCleared()
    {
        CurrentState = GameState.Settlement;
        GD.Print("所有房间清空！进入结算");

        // Unlock map-specific weapons
        if (WeaponUnlockPool.Instance != null)
        {
            string mapId = _roomGenerator?.CurrentMapId;
            if (!string.IsNullOrEmpty(mapId))
            {
                WeaponUnlockPool.Instance.UnlockWeaponsFromMap(mapId);
            }
        }

        // 胜利: 添加奖励紫色/金色记忆水晶
        AddBonusEngrams();

        SettleRun(true);
    }

    private void OnPlayerDied()
    {
        CurrentState = GameState.GameOver;
        SettleRun(false);
    }

    /// <summary>
    /// 统一结算流程 — 胜利和失败共用
    /// 转移微光到 MetaProgression，显示结算 UI
    /// </summary>
    private void SettleRun(bool isVictory)
    {
        GetTree().Paused = true;

        // 转移微光到持久化存储
        if (_glimmer > 0)
        {
            MetaProgression.Instance?.AddGlimmer(_glimmer);
            GD.Print($"结算微光: {_glimmer} 已转入 MetaProgression");
        }

        // 获取通关武器名称
        string weaponName = "无";
        if (_player?.Equipment?.CurrentWeapon?.Data != null)
        {
            weaponName = _player.Equipment.CurrentWeapon.Data.DisplayName;
        }

        // 创建并显示结算 UI
        var settlementUI = new SettlementUI();
        GetTree().CurrentScene.AddChild(settlementUI);
        settlementUI.SetData(isVictory, _killCount, _gameTime, _glimmer,
            _engramDecoder, weaponName);
    }

    /// <summary>
    /// 胜利时添加奖励记忆水晶（紫色/金色）
    /// </summary>
    private void AddBonusEngrams()
    {
        // 1-2 个高稀有度奖励水晶
        int bonusCount = GD.RandRange(1, 2);
        for (int i = 0; i < bonusCount; i++)
        {
            var rarity = GD.Randf() < 0.3f ? Rarity.Epic : Rarity.Rare;
            var weaponData = LootTable.GenerateWeapon(
                _roomGenerator?.CurrentRoom ?? 4, true);
            var engram = new EngramData(weaponData, rarity);
            _engramDecoder.CollectedEngrams.Add(engram);
        }
        GD.Print($"胜利奖励: 添加 {bonusCount} 个高稀有度记忆水晶");
    }

    public void AddGlimmer(int amount)
    {
        _glimmer += amount;
        EmitSignal(SignalName.GlimmerChanged, _glimmer);
    }

    public void AddKill()
    {
        _killCount++;
    }

    /// <summary>
    /// 收集记忆水晶，存储到解码器
    /// </summary>
    public void CollectEngram(MemoryEngram engram)
    {
        _engramDecoder.AddEngram(engram);
        GD.Print($"收集记忆水晶: [{engram.GetRarity()}]");
    }

    public EngramDecoder GetEngramDecoder() => _engramDecoder;

    public int GetGlimmer() => _glimmer;
    public float GetGameTime() => _gameTime;
    public int GetKillCount() => _killCount;

    public void ReturnToMainMenu()
    {
        GetTree().Paused = false;
        CurrentState = GameState.MainMenu;
        GetTree().ChangeSceneToFile("res://scenes/ui/MainMenu.tscn");
    }

    public void SpawnWeaponDrop(Vector2 position, WeaponData data)
    {
        var drop = new Node2D();
        drop.GlobalPosition = position + new Vector2(GD.RandRange(-20, 20), GD.RandRange(-20, 20));

        // 颜色按稀有度
        var color = data.Rarity switch
        {
            Rarity.Common => new Color(0.7f, 0.7f, 0.7f),
            Rarity.Uncommon => new Color(0.2f, 0.8f, 0.2f),
            Rarity.Rare => new Color(0.2f, 0.4f, 1.0f),
            Rarity.Epic => new Color(0.6f, 0.2f, 0.8f),
            Rarity.Legendary => new Color(1.0f, 0.8f, 0.0f),
            _ => Colors.White
        };

        // 掉落物视觉
        var visual = new ColorRect
        {
            Size = new Vector2(14, 14),
            Position = new Vector2(-7, -7),
            Color = color,
        };
        drop.AddChild(visual);

        // 武器名称提示（默认隐藏）
        var prompt = new Label();
        string perkHint = data.Perks.Count > 0 ? $" ({data.Perks.Count}特性)" : "";
        prompt.Text = $"[E] {data.DisplayName}{perkHint}";
        prompt.Position = new Vector2(-50, -30);
        prompt.AddThemeColorOverride("font_color", color);
        prompt.AddThemeFontSizeOverride("font_size", 13);
        prompt.Visible = false;
        prompt.ZIndex = 20;
        drop.AddChild(prompt);

        // 存储武器数据
        _dropData[drop] = data;
        _dropPrompts[drop] = prompt;

        GetTree().CurrentScene.AddChild(drop);
        _weaponDrops.Add(drop);
    }

    private readonly List<Node2D> _weaponDrops = new();
    private readonly Dictionary<Node2D, WeaponData> _dropData = new();
    private readonly Dictionary<Node2D, Label> _dropPrompts = new();

    public void CleanupWeaponDrops()
    {
        foreach (var drop in _weaponDrops)
        {
            if (IsInstanceValid(drop)) drop.QueueFree();
        }
        _weaponDrops.Clear();
        _dropData.Clear();
        _dropPrompts.Clear();
        _closestWeaponDrop = null;
    }

    public override void _Process(double delta)
    {
        if (CurrentState == GameState.Playing)
        {
            _gameTime += (float)delta;
        }

        // 检测玩家与武器掉落的距离，显示/隐藏交互提示
        if (_player == null) return;
        var playerPos = _player.GlobalPosition;
        Node2D closestDrop = null;
        float closestDist = 50f; // 交互距离

        for (int i = _weaponDrops.Count - 1; i >= 0; i--)
        {
            var drop = _weaponDrops[i];
            if (!IsInstanceValid(drop))
            {
                _weaponDrops.RemoveAt(i);
                _dropData.Remove(drop);
                _dropPrompts.Remove(drop);
                continue;
            }
            float dist = playerPos.DistanceTo(drop.GlobalPosition);
            if (_dropPrompts.TryGetValue(drop, out var prompt))
            {
                prompt.Visible = dist < closestDist;
            }
            if (dist < closestDist)
            {
                closestDist = dist;
                closestDrop = drop;
            }
        }
        _closestWeaponDrop = closestDrop;
    }

    private Node2D _closestWeaponDrop;

    public void TryInteract()
    {
        if (_closestWeaponDrop == null || !IsInstanceValid(_closestWeaponDrop)) return;
        if (!_dropData.TryGetValue(_closestWeaponDrop, out var data)) return;

        if (_player.Equipment?.CurrentWeapon != null)
        {
            // 已有武器，打开替换 UI（时停）
            ShowWeaponSwapUI(data, _closestWeaponDrop);
        }
        else
        {
            // 没有武器，直接装备
            _player.Equipment.EquipWeapon(data);
            _weaponDrops.Remove(_closestWeaponDrop);
            _closestWeaponDrop.QueueFree();
            _closestWeaponDrop = null;
        }
    }

    private void ShowWeaponSwapUI(WeaponData newData, Node2D drop)
    {
        GetTree().Paused = true;

        var layer = new CanvasLayer();
        layer.Layer = 10;
        layer.ProcessMode = Node.ProcessModeEnum.Always;
        GetTree().CurrentScene.AddChild(layer);

        layer.AddChild(UIStyle.Overlay(0.82f));

        // 标题
        var title = UIStyle.MakeLabel("发现新武器！", 24, UIStyle.AccentGold, true);
        title.Position = new Vector2((1280 - 180) / 2, 24);
        layer.AddChild(title);

        // 两个面板
        float panelW = 280, panelH = 340, gap = 40;
        float totalW = panelW * 2 + gap;
        float startX = (1280 - totalW) / 2;
        float panelY = 70;

        var currentData = _player.Equipment.CurrentWeapon.Data;
        var currentPanel = CreateWeaponPanel(currentData, "当前武器");
        currentPanel.Position = new Vector2(startX, panelY);
        currentPanel.Size = new Vector2(panelW, panelH);
        layer.AddChild(currentPanel);

        // VS 居中在 gap 之间
        float vsX = startX + panelW + (gap - 30) / 2;
        var vs = UIStyle.MakeLabel("VS", 28, UIStyle.AccentGold, true);
        vs.Position = new Vector2(vsX, panelY + panelH / 2 - 18);
        layer.AddChild(vs);

        var newPanel = CreateWeaponPanel(newData, "新武器");
        newPanel.Position = new Vector2(startX + panelW + gap, panelY);
        newPanel.Size = new Vector2(panelW, panelH);
        layer.AddChild(newPanel);

        // 按钮（居中）
        float btnY = panelY + panelH + 20;
        float btnW = 150, btnGap = 30;
        float btnStartX = (1280 - btnW * 2 - btnGap) / 2;

        var replaceBtn = UIStyle.MakeButton("替换武器", new Vector2(btnW, 42));
        replaceBtn.Position = new Vector2(btnStartX, btnY);
        replaceBtn.ProcessMode = Node.ProcessModeEnum.Always;
        layer.AddChild(replaceBtn);

        var keepBtn = UIStyle.MakeButton("保留当前", new Vector2(btnW, 42));
        keepBtn.Position = new Vector2(btnStartX + btnW + btnGap, btnY);
        keepBtn.ProcessMode = Node.ProcessModeEnum.Always;
        layer.AddChild(keepBtn);

        replaceBtn.Pressed += () =>
        {
            _player.Equipment.EquipWeapon(newData);
            _weaponDrops.Remove(drop);
            if (IsInstanceValid(drop)) drop.QueueFree();
            layer.QueueFree();
            GetTree().Paused = false;
        };
        keepBtn.Pressed += () => { layer.QueueFree(); GetTree().Paused = false; };
    }

    private Control CreateWeaponPanel(WeaponData data, string panelTitle)
    {
        var rc = UIStyle.RarityColor(data.Rarity);
        var panel = new Control();
        panel.ZIndex = 101;

        // 背景面板（带稀有度边框）
        var bg = UIStyle.Panel(new Vector2(280, 340), rc);
        panel.AddChild(bg);

        float y = 4;
        float x = 8;
        float w = 248;

        Control L(string text, int fontSize, Color? color = null, bool bold = false)
        {
            var l = UIStyle.MakeLabel(text, fontSize, color, bold);
            l.Position = new Vector2(x, y);
            l.Size = new Vector2(w, 22);
            bg.AddChild(l);
            y += fontSize + 8;
            return l;
        }

        void Sep()
        {
            bg.AddChild(UIStyle.Separator(new Vector2(x, y + 2), w));
            y += 10;
        }

        L(panelTitle, 13, UIStyle.TextMuted);
        L($"[{UIStyle.RarityName(data.Rarity)}] {data.DisplayName}", 20, rc, true);

        string typeName = data.Type switch
        {
            WeaponType.AutoRifle => "自动步枪",
            WeaponType.Shotgun => "霰弹枪",
            WeaponType.HandCannon => "手炮",
            _ => "武器"
        };
        L(typeName, 14, UIStyle.TextSecondary);

        Sep();

        L($"伤害: {data.BaseDamage}", 16);
        L($"射速: {data.FireRate:F1}/s", 16);
        if (data.BulletCount > 1) L($"弹丸数: {data.BulletCount}", 16);
        if (data.KnockbackForce > 0) L($"击退: {data.KnockbackForce:F0}", 16);

        if (data.Perks.Count > 0)
        {
            Sep();
            L("特性:", 14, UIStyle.AccentGold, true);
            foreach (var perk in data.Perks)
            {
                if (PerkSystem.PerkInfo.TryGetValue(perk, out var info))
                {
                    L($"• {info.Name}", 14, UIStyle.AccentCyan);
                    var desc = L(info.Desc, 11, UIStyle.TextMuted);
                    if (desc is Label descLabel)
                    {
                        descLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
                        descLabel.Size = new Vector2(w, 40);
                    }
                    y += 18;
                }
            }
        }

        return panel;
    }
}
