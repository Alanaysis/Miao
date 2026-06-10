using Godot;
using System.Collections.Generic;
using Miao.Armor;
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

    // Info display
    private Label _lightLevelLabel;
    private Label _statsLabel;
    private VBoxContainer _modList;

    // Subclass selector
    private HBoxContainer _subclassSelector;

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

        // Right panel: equipment slots
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

        rightVbox.AddChild(actionBox);

        mainHbox.AddChild(rightVbox);

        panel.AddChild(mainHbox);

        Hide();
    }

    public void Open()
    {
        RefreshDisplay();
        Show();
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

    private void OnStartGame()
    {
        Hide();
        GameManager.Instance.StartGame();
    }

    private void OnBack()
    {
        Hide();
    }
}
