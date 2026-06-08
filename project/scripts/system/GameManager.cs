using Godot;
using System.Collections.Generic;
using Miao.Weapon;

namespace Miao.System;

public partial class GameManager : Node
{
    public static GameManager Instance { get; private set; }

    public enum GameState { MainMenu, Playing, RoomTransition, Boss, Settlement, GameOver }
    public GameState CurrentState { get; private set; }

    private Player.Player _player;
    private RoomGenerator _roomGenerator;
    private float _gameTime;
    private int _killCount;
    private int _glimmer;

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

        // 右下角小面板
        float panelW = 260, panelH = 120;
        float px = 1280 - panelW - 20;
        float py = 720 - panelH - 20;

        var bg = new ColorRect();
        bg.Position = new Vector2(px, py);
        bg.Size = new Vector2(panelW, panelH);
        bg.Color = new Color(0.1f, 0.1f, 0.15f, 0.9f);
        _roomClearUI.AddChild(bg);

        var title = new Label();
        title.Text = $"✓ 房间 {roomIndex + 1} 已清空！";
        title.Position = new Vector2(px + 16, py + 12);
        title.AddThemeFontSizeOverride("font_size", 16);
        title.AddThemeColorOverride("font_color", new Color(0.4f, 1.0f, 0.4f));
        _roomClearUI.AddChild(title);

        bool isLastRoom = roomIndex + 1 >= _roomGenerator.TotalRooms;
        string btnText = isLastRoom ? "结算" : "进入下一房间";

        var enterBtn = new Button();
        enterBtn.Text = btnText;
        enterBtn.Position = new Vector2(px + 16, py + 50);
        enterBtn.Size = new Vector2(120, 36);
        _roomClearUI.AddChild(enterBtn);

        var waitBtn = new Button();
        waitBtn.Text = "稍后";
        waitBtn.Position = new Vector2(px + 146, py + 50);
        waitBtn.Size = new Vector2(100, 36);
        _roomClearUI.AddChild(waitBtn);

        enterBtn.ProcessMode = Node.ProcessModeEnum.Always;
        waitBtn.ProcessMode = Node.ProcessModeEnum.Always;

        enterBtn.Pressed += () =>
        {
            HideRoomClearPrompt();
            NextRoom();
        };

        waitBtn.Pressed += () =>
        {
            HideRoomClearPrompt();
        };
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
    }

    private void OnPlayerDied()
    {
        CurrentState = GameState.GameOver;
        GetTree().Paused = true;
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

        float W = 1280, H = 720;

        // CanvasLayer 保证 UI 在屏幕固定位置，不受相机影响
        var layer = new CanvasLayer();
        layer.Layer = 10;
        layer.ProcessMode = Node.ProcessModeEnum.Always;
        GetTree().CurrentScene.AddChild(layer);

        // 全屏遮罩
        var bg = new ColorRect();
        bg.Size = new Vector2(W, H);
        bg.Color = new Color(0, 0, 0, 0.75f);
        bg.MouseFilter = Control.MouseFilterEnum.Stop;
        layer.AddChild(bg);

        // 标题
        var title = new Label();
        title.Text = "⚔ 发现新武器！";
        title.Position = new Vector2(W / 2 - 80, 40);
        title.AddThemeFontSizeOverride("font_size", 22);
        bg.AddChild(title);

        // 当前武器面板
        float panelW = 260, panelH = 340;
        float gap = 60;
        float totalW = panelW * 2 + gap;
        float startX = (W - totalW) / 2;
        float panelY = 90;

        var currentData = _player.Equipment.CurrentWeapon.Data;
        var currentPanel = CreateWeaponPanel(currentData, "当前武器");
        currentPanel.Position = new Vector2(startX, panelY);
        currentPanel.Size = new Vector2(panelW, panelH);
        bg.AddChild(currentPanel);

        // VS
        var vs = new Label();
        vs.Text = "VS";
        vs.Position = new Vector2(W / 2 - 15, panelY + panelH / 2 - 15);
        vs.AddThemeFontSizeOverride("font_size", 26);
        vs.AddThemeColorOverride("font_color", new Color(1, 0.8f, 0.2f));
        bg.AddChild(vs);

        // 新武器面板
        var newPanel = CreateWeaponPanel(newData, "新武器");
        newPanel.Position = new Vector2(startX + panelW + gap, panelY);
        newPanel.Size = new Vector2(panelW, panelH);
        bg.AddChild(newPanel);

        // 按钮
        float btnY = panelY + panelH + 30;
        var replaceBtn = new Button();
        replaceBtn.Text = "替换武器";
        replaceBtn.Position = new Vector2(W / 2 - 170, btnY);
        replaceBtn.Size = new Vector2(150, 42);
        bg.AddChild(replaceBtn);

        var keepBtn = new Button();
        keepBtn.Text = "保留当前";
        keepBtn.Position = new Vector2(W / 2 + 20, btnY);
        keepBtn.Size = new Vector2(150, 42);
        bg.AddChild(keepBtn);

        replaceBtn.Pressed += () =>
        {
            _player.Equipment.EquipWeapon(newData);
            _weaponDrops.Remove(drop);
            if (IsInstanceValid(drop)) drop.QueueFree();
            layer.QueueFree();
            GetTree().Paused = false;
        };

        keepBtn.Pressed += () =>
        {
            layer.QueueFree();
            GetTree().Paused = false;
        };
    }

    private Control CreateWeaponPanel(WeaponData data, string title)
    {
        var panel = new Control();
        panel.ZIndex = 101;

        // 背景
        var bg = new ColorRect();
        bg.Size = new Vector2(260, 340);
        bg.Color = new Color(0.12f, 0.12f, 0.18f, 0.95f);
        panel.AddChild(bg);

        float y = 12;
        float x = 16;
        float w = 228;

        var rarityColor = data.Rarity switch
        {
            Rarity.Common => new Color(0.7f, 0.7f, 0.7f),
            Rarity.Uncommon => new Color(0.2f, 0.8f, 0.2f),
            Rarity.Rare => new Color(0.2f, 0.4f, 1.0f),
            Rarity.Epic => new Color(0.6f, 0.2f, 0.8f),
            Rarity.Legendary => new Color(1.0f, 0.8f, 0.0f),
            _ => Colors.White
        };

        string rarityName = data.Rarity switch
        {
            Rarity.Common => "普通",
            Rarity.Uncommon => "优秀",
            Rarity.Rare => "稀有",
            Rarity.Epic => "史诗",
            Rarity.Legendary => "传说",
            _ => ""
        };

        Label MakeLabel(string text, int fontSize, Color? color = null)
        {
            var l = new Label();
            l.Text = text;
            l.Position = new Vector2(x, y);
            l.Size = new Vector2(w, 20);
            l.AddThemeFontSizeOverride("font_size", fontSize);
            if (color.HasValue) l.AddThemeColorOverride("font_color", color.Value);
            panel.AddChild(l);
            y += fontSize + 8;
            return l;
        }

        // 分隔线
        void MakeSep()
        {
            var sep = new ColorRect();
            sep.Position = new Vector2(x, y + 2);
            sep.Size = new Vector2(w, 1);
            sep.Color = new Color(0.4f, 0.4f, 0.4f, 0.5f);
            panel.AddChild(sep);
            y += 10;
        }

        MakeLabel(title, 14, new Color(0.6f, 0.6f, 0.6f));
        MakeLabel($"[{rarityName}] {data.DisplayName}", 20, rarityColor);

        string typeName = data.Type switch
        {
            WeaponType.AutoRifle => "自动步枪",
            WeaponType.Shotgun => "霰弹枪",
            WeaponType.HandCannon => "手炮",
            _ => "武器"
        };
        MakeLabel(typeName, 14);

        MakeSep();

        MakeLabel($"伤害: {data.BaseDamage}", 16);
        MakeLabel($"射速: {data.FireRate:F1}/s", 16);

        if (data.BulletCount > 1)
            MakeLabel($"弹丸数: {data.BulletCount}", 16);

        if (data.KnockbackForce > 0)
            MakeLabel($"击退: {data.KnockbackForce:F0}", 16);

        if (data.Perks.Count > 0)
        {
            MakeSep();
            MakeLabel("特性:", 14, new Color(0.9f, 0.8f, 0.4f));

            foreach (var perk in data.Perks)
            {
                if (PerkSystem.PerkInfo.TryGetValue(perk, out var info))
                {
                    MakeLabel($"• {info.Name}", 14, new Color(0.8f, 0.9f, 1.0f));
                    var desc = MakeLabel(info.Desc, 11, new Color(0.5f, 0.5f, 0.5f));
                    desc.AutowrapMode = TextServer.AutowrapMode.WordSmart;
                    desc.Size = new Vector2(w, 40);
                    y += 18; // 额外行距
                }
            }
        }

        return panel;
    }
}
