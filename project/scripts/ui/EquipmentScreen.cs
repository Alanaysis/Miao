using Godot;
using System.Collections.Generic;
using System.Linq;
using Miao.Armor;
using Miao.Data;
using Miao.Player;
using Miao.System;
using Miao.Weapon;

namespace Miao.UI;

public partial class EquipmentScreen : Control
{
    // Slot buttons
    private Button _weaponSlotBtn;
    private Dictionary<ArmorSlot, Button> _armorSlotBtns = new();
    private Button _subclassBtn;

    // Aspect slot buttons
    private Button _aspectSlot1Btn;
    private Button _aspectSlot2Btn;

    // Info display
    private Label _lightLevelLabel;
    private Label _statsLabel;
    private VBoxContainer _modList;

    // Subclass selector
    private HBoxContainer _subclassSelector;

    // Close button (in-game mode)
    private Button _closeBtn;

    // Map selection
    private string _selectedMapId = "nest";
    private VBoxContainer _mapListContainer;

    // In-game state
    private bool _isInGameMode = false;

    /// <summary>是否处于游戏内只读模式</summary>
    public bool IsInGameMode() => _isInGameMode;

    // Current state
    private WeaponData _currentWeapon;
    private string _currentClass = "hunter";
    private SubclassType _currentSubclass = SubclassType.Void;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;

        // Dark overlay background
        var overlay = UIStyle.Overlay(0.92f);
        AddChild(overlay);

        var panel = UIStyle.Panel(new Vector2(1000, 600));
        panel.Position = new Vector2(140, 60);
        AddChild(panel);

        // Main horizontal layout
        var mainHbox = new HBoxContainer();
        mainHbox.AddThemeConstantOverride("separation", 12);

        // Left panel: character + subclass
        var leftVbox = new VBoxContainer();
        leftVbox.CustomMinimumSize = new Vector2(250, 0);
        leftVbox.AddThemeConstantOverride("separation", 6);

        var titleLabel = UIStyle.MakeLabel("装备配置", 24, UIStyle.AccentBlue, true);
        titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
        leftVbox.AddChild(titleLabel);

        leftVbox.AddChild(UIStyle.Separator(Vector2.Zero, 220));

        // Character class display
        var classLabel = UIStyle.MakeLabel($"职业: {_currentClass}", 16, UIStyle.TextSecondary);
        leftVbox.AddChild(classLabel);

        // Subclass selector
        leftVbox.AddChild(UIStyle.MakeLabel("子类:", 14, UIStyle.TextMuted));
        _subclassSelector = new HBoxContainer();
        _subclassSelector.AddThemeConstantOverride("separation", 6);
        foreach (SubclassType type in System.Enum.GetValues<SubclassType>())
        {
            if (type == SubclassType.Stasis) continue; // Not implemented yet
            var btn = UIStyle.MakeButton(type switch
            {
                SubclassType.Void => "虚空",
                SubclassType.Arc => "电弧",
                SubclassType.Solar => "烈日",
                _ => type.ToString()
            }, new Vector2(70, 30));
            SubclassType capturedType = type;
            btn.Pressed += () => OnSubclassSelected(capturedType);
            _subclassSelector.AddChild(btn);
        }
        leftVbox.AddChild(_subclassSelector);

        leftVbox.AddChild(UIStyle.Separator(Vector2.Zero, 220));

        // Aspect slots
        leftVbox.AddChild(UIStyle.MakeLabel("星相槽位:", 14, UIStyle.TextMuted));

        var aspectRow1 = new HBoxContainer();
        aspectRow1.AddThemeConstantOverride("separation", 6);
        aspectRow1.AddChild(UIStyle.MakeLabel("槽位1:", 12, UIStyle.TextSecondary));
        _aspectSlot1Btn = UIStyle.MakeButton("[空]", new Vector2(160, 30));
        _aspectSlot1Btn.Pressed += () => OnAspectSlotClicked(1);
        aspectRow1.AddChild(_aspectSlot1Btn);
        leftVbox.AddChild(aspectRow1);

        var aspectRow2 = new HBoxContainer();
        aspectRow2.AddThemeConstantOverride("separation", 6);
        aspectRow2.AddChild(UIStyle.MakeLabel("槽位2:", 12, UIStyle.TextSecondary));
        _aspectSlot2Btn = UIStyle.MakeButton("[空]", new Vector2(160, 30));
        _aspectSlot2Btn.Pressed += () => OnAspectSlotClicked(2);
        aspectRow2.AddChild(_aspectSlot2Btn);
        leftVbox.AddChild(aspectRow2);

        leftVbox.AddChild(UIStyle.Separator(Vector2.Zero, 220));

        // Light level
        _lightLevelLabel = UIStyle.MakeLabel("光等: 0", 20, UIStyle.AccentGold, true);
        leftVbox.AddChild(_lightLevelLabel);

        // Stats summary
        _statsLabel = UIStyle.MakeLabel("", 12, UIStyle.TextSecondary);
        leftVbox.AddChild(_statsLabel);

        leftVbox.AddChild(UIStyle.Separator(Vector2.Zero, 220));

        // Mod list
        leftVbox.AddChild(UIStyle.MakeLabel("已装备Mod:", 14, UIStyle.TextMuted));
        _modList = new VBoxContainer();
        _modList.AddThemeConstantOverride("separation", 2);
        leftVbox.AddChild(_modList);

        mainHbox.AddChild(leftVbox);
        mainHbox.AddChild(new VSeparator());

        // Right panel: equipment slots + map selection
        var rightVbox = new VBoxContainer();
        rightVbox.AddThemeConstantOverride("separation", 6);

        rightVbox.AddChild(UIStyle.MakeLabel("装备槽位", 18, UIStyle.AccentBlue, true));
        rightVbox.AddChild(UIStyle.Separator(Vector2.Zero, 400));

        // Weapon slot
        var weaponRow = new HBoxContainer();
        weaponRow.AddThemeConstantOverride("separation", 10);
        weaponRow.AddChild(UIStyle.MakeLabel("武器:", 14, UIStyle.TextSecondary));
        _weaponSlotBtn = UIStyle.MakeButton("[空]", new Vector2(300, 40));
        _weaponSlotBtn.Pressed += OnWeaponSlotClicked;
        weaponRow.AddChild(_weaponSlotBtn);
        rightVbox.AddChild(weaponRow);

        rightVbox.AddChild(UIStyle.Separator(Vector2.Zero, 400));

        // Armor slots
        foreach (ArmorSlot slot in System.Enum.GetValues<ArmorSlot>())
        {
            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 10);
            string slotName = slot switch
            {
                ArmorSlot.Head => "头盔:",
                ArmorSlot.Chest => "胸甲:",
                ArmorSlot.Hands => "手套:",
                ArmorSlot.Legs => "腿甲:",
                ArmorSlot.ClassItem => "职业物品:",
                _ => slot.ToString()
            };
            row.AddChild(UIStyle.MakeLabel(slotName, 14, UIStyle.TextSecondary));

            var btn = UIStyle.MakeButton("[空]", new Vector2(300, 40));
            ArmorSlot capturedSlot = slot;
            btn.Pressed += () => OnArmorSlotClicked(capturedSlot);
            _armorSlotBtns[slot] = btn;
            row.AddChild(btn);
            rightVbox.AddChild(row);
        }

        rightVbox.AddChild(UIStyle.Separator(Vector2.Zero, 400));

        // Map selection section
        AddMapSelection(rightVbox);

        rightVbox.AddChild(UIStyle.Separator(Vector2.Zero, 400));

        // Action buttons
        var actionBox = new HBoxContainer();
        actionBox.Alignment = BoxContainer.AlignmentMode.Center;
        actionBox.AddThemeConstantOverride("separation", 20);

        var startBtn = UIStyle.MakeButton("出发！", new Vector2(120, 40));
        startBtn.Pressed += OnStartGame;
        actionBox.AddChild(startBtn);

        var backBtn = UIStyle.MakeButton("返回", new Vector2(120, 40));
        backBtn.Pressed += OnBack;
        actionBox.AddChild(backBtn);

        _closeBtn = UIStyle.MakeButton("关闭 (Tab/ESC)", new Vector2(160, 40));
        _closeBtn.Pressed += CloseInGame;
        _closeBtn.Visible = false;
        actionBox.AddChild(_closeBtn);

        rightVbox.AddChild(actionBox);

        mainHbox.AddChild(rightVbox);

        panel.AddChild(mainHbox);

        Hide();
    }

    /// <summary>
    /// 添加地图选择区域
    /// </summary>
    private void AddMapSelection(VBoxContainer parent)
    {
        parent.AddChild(UIStyle.MakeLabel("选择地图", 16, UIStyle.AccentGold, true));
        parent.AddChild(UIStyle.Separator(Vector2.Zero, 400));

        _mapListContainer = new VBoxContainer();
        _mapListContainer.AddThemeConstantOverride("separation", 4);

        var mapsData = DataLoader.Load<MapsConfig>("maps.json");
        if (mapsData?.Maps == null)
        {
            _mapListContainer.AddChild(UIStyle.MakeLabel("无可用地图", 12, UIStyle.TextMuted));
            parent.AddChild(_mapListContainer);
            return;
        }

        foreach (var map in mapsData.Maps)
        {
            var hbox = new HBoxContainer();
            hbox.AddThemeConstantOverride("separation", 8);

            var btn = new Button();
            string stars = new string('★', map.Difficulty);
            btn.Text = $"{map.Name} {stars}";
            btn.CustomMinimumSize = new Vector2(250, 35);
            string capturedId = map.Id;
            btn.Pressed += () => OnMapSelected(capturedId);

            // 样式
            var style = new StyleBoxFlat();
            style.BgColor = capturedId == _selectedMapId ? new Color("#1e3a5f") : new Color("#1e293b");
            style.BorderColor = capturedId == _selectedMapId ? UIStyle.AccentCyan : new Color("#3b82f6", 0.5f);
            style.BorderWidthLeft = 1; style.BorderWidthRight = 1;
            style.BorderWidthTop = 1; style.BorderWidthBottom = 1;
            style.CornerRadiusTopLeft = 4; style.CornerRadiusTopRight = 4;
            style.CornerRadiusBottomLeft = 4; style.CornerRadiusBottomRight = 4;
            style.ContentMarginLeft = 8; style.ContentMarginRight = 8;
            style.ContentMarginTop = 4; style.ContentMarginBottom = 4;
            btn.AddThemeStyleboxOverride("normal", style);

            hbox.AddChild(btn);

            // 难度标签
            var diffLabel = UIStyle.MakeLabel($"难度 {map.Difficulty}", 11,
                map.Difficulty <= 2 ? UIStyle.AccentGreen :
                map.Difficulty <= 4 ? UIStyle.AccentGold : UIStyle.AccentRed);
            hbox.AddChild(diffLabel);

            _mapListContainer.AddChild(hbox);
        }

        parent.AddChild(_mapListContainer);
    }

    /// <summary>
    /// 刷新地图选择按钮的高亮状态
    /// </summary>
    private void RefreshMapSelection()
    {
        if (_mapListContainer == null) return;

        var mapsData = DataLoader.Load<MapsConfig>("maps.json");
        if (mapsData?.Maps == null) return;

        int index = 0;
        foreach (var child in _mapListContainer.GetChildren())
        {
            if (child is HBoxContainer hbox && index < mapsData.Maps.Count)
            {
                var map = mapsData.Maps[index];
                foreach (var hboxChild in hbox.GetChildren())
                {
                    if (hboxChild is Button btn)
                    {
                        bool isSelected = map.Id == _selectedMapId;
                        var style = new StyleBoxFlat();
                        style.BgColor = isSelected ? new Color("#1e3a5f") : new Color("#1e293b");
                        style.BorderColor = isSelected ? UIStyle.AccentCyan : new Color("#3b82f6", 0.5f);
                        style.BorderWidthLeft = 1; style.BorderWidthRight = 1;
                        style.BorderWidthTop = 1; style.BorderWidthBottom = 1;
                        style.CornerRadiusTopLeft = 4; style.CornerRadiusTopRight = 4;
                        style.CornerRadiusBottomLeft = 4; style.CornerRadiusBottomRight = 4;
                        style.ContentMarginLeft = 8; style.ContentMarginRight = 8;
                        style.ContentMarginTop = 4; style.ContentMarginBottom = 4;
                        btn.AddThemeStyleboxOverride("normal", style);
                    }
                }
                index++;
            }
        }
    }

    private void OnMapSelected(string mapId)
    {
        _selectedMapId = mapId;
        GD.Print($"选择地图: {mapId}");
        RefreshMapSelection();
    }

    public void Open()
    {
        _isInGameMode = false;
        RefreshDisplay();
        Show();
    }

    /// <summary>
    /// 以游戏内只读模式打开装备界面（Tab 键触发，暂停游戏）
    /// </summary>
    public void OpenInGame()
    {
        _isInGameMode = true;
        GetTree().Paused = true;
        RefreshDisplay();

        // 禁用编辑按钮（只读模式）
        _weaponSlotBtn.Disabled = true;
        foreach (var btn in _armorSlotBtns.Values)
        {
            btn.Disabled = true;
        }
        _aspectSlot1Btn.Disabled = true;
        _aspectSlot2Btn.Disabled = true;
        _subclassSelector.Visible = false;

        // 隐藏主菜单按钮，显示关闭按钮
        // 遍历 actionBox 隐藏出发和返回按钮
        var actionBox = _closeBtn.GetParent<HBoxContainer>();
        foreach (var child in actionBox.GetChildren())
        {
            if (child is Button btn && btn != _closeBtn)
            {
                btn.Visible = false;
            }
        }
        _closeBtn.Visible = true;

        Show();
    }

    /// <summary>
    /// 关闭游戏内装备界面，恢复游戏
    /// </summary>
    public void CloseInGame()
    {
        if (!_isInGameMode) return;

        _isInGameMode = false;
        GetTree().Paused = false;
        Hide();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!_isInGameMode) return;

        if (@event.IsActionPressed("ui_cancel") || @event.IsActionPressed("equipment"))
        {
            CloseInGame();
            GetViewport().SetInputAsHandled();
        }
    }

    private void RefreshDisplay()
    {
        // Update weapon slot
        if (_currentWeapon != null)
        {
            var rarityName = UIStyle.RarityName(_currentWeapon.Rarity);
            _weaponSlotBtn.Text = $"[{rarityName}] {_currentWeapon.DisplayName}";
        }
        else
        {
            _weaponSlotBtn.Text = "[空]";
        }

        // Update armor slots from player
        var player = GetTree().GetFirstNodeInGroup("player") as Player;
        if (player?.Armors != null)
        {
            foreach (var kvp in player.Armors.Slots)
            {
                if (_armorSlotBtns.ContainsKey(kvp.Key))
                {
                    var btn = _armorSlotBtns[kvp.Key];
                    if (kvp.Value.HasArmor())
                    {
                        var armor = kvp.Value.EquippedArmor;
                        string rarityDisplay = armor.Rarity ?? "普通";
                        btn.Text = $"[{rarityDisplay}] {armor.Name} (光等{armor.LightLevel})";
                    }
                    else
                    {
                        btn.Text = "[空]";
                    }
                }
            }

            // Update light level
            int lightLevel = LightLevelCalculator.CalculateTotalLightLevel(_currentWeapon, player.Armors);
            _lightLevelLabel.Text = $"光等: {lightLevel}";

            // Update stats
            int hp = player.MaxHealth + player.Armors.GetTotalHealthBonus();
            int armor = player.Armors.GetTotalArmorBonus();
            float speed = player.MoveSpeed + player.Armors.GetTotalMoveSpeedBonus();
            _statsLabel.Text = $"血量: {hp} | 护甲: {armor} | 移速: {speed:F1}";

            // Update mod list
            foreach (var child in _modList.GetChildren()) child.QueueFree();
            var mods = player.Armors.GetAllEquippedMods();
            if (mods.Count == 0)
            {
                _modList.AddChild(UIStyle.MakeLabel("无", 12, UIStyle.TextMuted));
            }
            else
            {
                foreach (var modName in mods)
                {
                    _modList.AddChild(UIStyle.MakeLabel($"- {modName}", 12, UIStyle.AccentCyan));
                }
            }
        }

        // Update aspect slot buttons
        if (player?.Aspects != null)
        {
            var a1 = player.Aspects.EquippedAspect1;
            var a2 = player.Aspects.EquippedAspect2;
            _aspectSlot1Btn.Text = a1 != null ? a1.Name : "[空]";
            _aspectSlot2Btn.Text = a2 != null ? a2.Name : "[空]";
        }

        // Update subclass selector visual feedback
        foreach (var child in _subclassSelector.GetChildren())
        {
            if (child is Button btn)
            {
                // Highlight selected subclass by checking the label text
                bool isSelected = false;
                string btnLabel = btn.Text;
                if ((_currentSubclass == SubclassType.Void && btnLabel == "虚空") ||
                    (_currentSubclass == SubclassType.Arc && btnLabel == "电弧") ||
                    (_currentSubclass == SubclassType.Solar && btnLabel == "烈日"))
                {
                    isSelected = true;
                }

                // Update border color to indicate selection
                var style = new StyleBoxFlat();
                style.BgColor = isSelected ? new Color("#1e3a5f") : new Color("#1e293b");
                style.BorderColor = isSelected ? UIStyle.AccentCyan : new Color("#3b82f6", 0.5f);
                style.BorderWidthLeft = 1; style.BorderWidthRight = 1;
                style.BorderWidthTop = 1; style.BorderWidthBottom = 1;
                style.CornerRadiusTopLeft = 4; style.CornerRadiusTopRight = 4;
                style.CornerRadiusBottomLeft = 4; style.CornerRadiusBottomRight = 4;
                style.ContentMarginLeft = 8; style.ContentMarginRight = 8;
                style.ContentMarginTop = 4; style.ContentMarginBottom = 4;
                btn.AddThemeStyleboxOverride("normal", style);
            }
        }
    }

    private void OnSubclassSelected(SubclassType type)
    {
        _currentSubclass = type;

        // Apply subclass change via SubclassManager if available
        var subclassManager = GetNodeOrNull<SubclassManager>("/root/SubclassManager");
        subclassManager?.SetSubclass(type);

        RefreshDisplay();
    }

    private void OnWeaponSlotClicked()
    {
        // TODO: show weapon selection popup
        GD.Print("武器槽位点击 - 待实现武器选择界面");
    }

    private void OnArmorSlotClicked(ArmorSlot slot)
    {
        // TODO: show armor selection popup
        GD.Print($"护甲槽位点击: {slot} - 待实现护甲选择界面");
    }

    private void OnAspectSlotClicked(int slot)
    {
        var player = GetTree().GetFirstNodeInGroup("player") as Player;
        if (player?.Aspects == null || player?.Subclass == null) return;

        // If there's already an aspect in this slot, unequip it
        var equipped = slot == 1 ? player.Aspects.EquippedAspect1 : player.Aspects.EquippedAspect2;
        if (equipped != null)
        {
            player.Aspects.UnequipAspect(slot);
            RefreshDisplay();
            return;
        }

        // Otherwise, try to equip the first available unlocked aspect
        string className = "hunter";
        string subclassKey = player.Subclass.ActiveSubclass.ToString().ToLower();
        var available = player.Aspects.GetAvailableAspects(className, subclassKey);

        // Filter to unlocked aspects only
        var meta = MetaProgression.Instance;
        if (meta != null)
        {
            available = available.Where(a => meta.IsAspectUnlocked(a.Id)).ToList();
        }

        // Find one that isn't already equipped in the other slot
        var otherSlot = slot == 1 ? player.Aspects.EquippedAspect2 : player.Aspects.EquippedAspect1;
        var candidate = available.FirstOrDefault(a => otherSlot == null || a.Id != otherSlot.Id);

        if (candidate != null)
        {
            player.Aspects.EquipAspect(candidate, slot);
        }
        else
        {
            GD.Print($"没有可用的星相 (槽位 {slot})");
        }

        RefreshDisplay();
    }

    private void OnStartGame()
    {
        Hide();
        GameManager.Instance.SelectedMapId = _selectedMapId;
        GameManager.Instance.StartGame();
    }

    private void OnBack()
    {
        Hide();
    }
}
