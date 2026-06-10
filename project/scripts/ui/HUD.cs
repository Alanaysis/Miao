using Godot;
using Miao.Player;
using Miao.System;
using Miao.Weapon;

namespace Miao.UI;

public partial class HUD : CanvasLayer
{
    private ProgressBar _healthBar;
    private ProgressBar _shieldBar;
    private ProgressBar _superBar;
    private Label _hpLabel;
    private Label _weaponNameLabel;
    private Label _weaponStatLabel;
    private Label _perksLabel;
    private Label _roomLabel;
    private Label _killLabel;
    private Control _skill1Slot;
    private Control _skill2Slot;
    private Label _superLabel;
    private Label _lightLevelLabel;
    private Player.Player _player;

    public override void _Ready()
    {
        BuildTopLeft();
        BuildTopRight();
        BuildBottomLeft();
        BuildBottomRight();

        if (_player == null)
        {
            var node = GetTree().GetFirstNodeInGroup("player");
            if (node is Player.Player p) SetPlayer(p);
        }
    }

    public void SetPlayer(Player.Player player)
    {
        _player = player;
        player.HealthChanged += OnHealthChanged;
        player.ShieldChanged += OnShieldChanged;
        player.SuperChargeChanged += OnSuperChargeChanged;
    }

    // ==================== 左上：血条 ====================
    // 面板: (16,12) size(260,56) → ends (276,68)
    private void BuildTopLeft()
    {
        var panel = UIStyle.Panel(new Vector2(260, 56));
        panel.Position = new Vector2(16, 12);
        AddChild(panel);

        _hpLabel = UIStyle.LabelAt(new Vector2(8, 2), "100/100", 13, UIStyle.TextSecondary);
        panel.AddChild(_hpLabel);

        _healthBar = UIStyle.Bar(new Vector2(8, 20), new Vector2(228, 16), UIStyle.HealthGreen);
        panel.AddChild(_healthBar);

        _shieldBar = UIStyle.Bar(new Vector2(8, 40), new Vector2(228, 8), UIStyle.AccentCyan);
        panel.AddChild(_shieldBar);
    }

    // ==================== 右上：房间/击杀/光等 ====================
    // 面板: (1060,12) size(200,66) → ends (1260,78)
    private void BuildTopRight()
    {
        var panel = UIStyle.Panel(new Vector2(200, 66));
        panel.Position = new Vector2(1060, 12);
        AddChild(panel);

        _lightLevelLabel = UIStyle.LabelAt(new Vector2(8, 2), "光等: 0", 14, UIStyle.AccentGold, true);
        panel.AddChild(_lightLevelLabel);

        _roomLabel = UIStyle.LabelAt(new Vector2(8, 22), "房间 1/5", 14, UIStyle.AccentBlue, true);
        panel.AddChild(_roomLabel);

        _killLabel = UIStyle.LabelAt(new Vector2(8, 42), "击杀: 0", 12, UIStyle.TextSecondary);
        panel.AddChild(_killLabel);
    }

    // ==================== 左下：技能 + 超能 ====================
    // 技能: (16,648) size(48,48) → ends (64,696) and (76,648) → ends (124,696)
    // 超能: (16,650) size(130,24) → ends (146,674)
    private void BuildBottomLeft()
    {
        _skill1Slot = UIStyle.SkillSlot(new Vector2(16, 648), "Q", UIStyle.AccentBlue);
        AddChild(_skill1Slot);

        _skill2Slot = UIStyle.SkillSlot(new Vector2(76, 648), "R", UIStyle.AccentPurple);
        AddChild(_skill2Slot);

        // 超能条（放在技能方块右边，同一行）
        var superPanel = UIStyle.Panel(new Vector2(130, 48), UIStyle.AccentGold);
        superPanel.Position = new Vector2(136, 648);
        AddChild(superPanel);

        _superBar = UIStyle.Bar(new Vector2(8, 6), new Vector2(110, 10), UIStyle.AccentGold);
        superPanel.AddChild(_superBar);

        _superLabel = UIStyle.LabelAt(new Vector2(8, 22), "0/100", 12, UIStyle.AccentGold);
        superPanel.AddChild(_superLabel);
    }

    // ==================== 右下：武器面板 ====================
    // 面板: (960,640) size(300,68) → ends (1260,708)
    private void BuildBottomRight()
    {
        var panel = UIStyle.Panel(new Vector2(300, 68));
        panel.Position = new Vector2(960, 640);
        AddChild(panel);

        _weaponNameLabel = UIStyle.LabelAt(new Vector2(8, 2), "无武器", 16, UIStyle.TextMuted, true);
        panel.AddChild(_weaponNameLabel);

        _weaponStatLabel = UIStyle.LabelAt(new Vector2(8, 24), "", 12, UIStyle.TextSecondary);
        panel.AddChild(_weaponStatLabel);

        _perksLabel = UIStyle.LabelAt(new Vector2(8, 42), "", 11, UIStyle.AccentCyan);
        panel.AddChild(_perksLabel);
    }

    // ==================== 每帧更新 ====================
    public override void _Process(double delta)
    {
        if (_player == null) return;

        UpdateSkillSlot(_skill1Slot, _player.Skill1Timer);
        UpdateSkillSlot(_skill2Slot, _player.Skill2Timer);

        // 超能
        if (_player.IsSuperReady)
        {
            _superLabel.Text = "就绪!";
            _superLabel.AddThemeColorOverride("font_color", UIStyle.AccentGold);
            UIStyle.UpdateBarColor(_superBar, UIStyle.AccentGold);
        }
        else
        {
            _superLabel.Text = $"{_player.SuperCharge:F0}/{_player.SuperMaxCharge:F0}";
            _superLabel.AddThemeColorOverride("font_color", UIStyle.TextSecondary);
            UIStyle.UpdateBarColor(_superBar, new Color("#92700a"));
        }

        // 武器
        var weapon = _player.Equipment?.CurrentWeapon?.Data;
        if (weapon != null)
        {
            var rc = UIStyle.RarityColor(weapon.Rarity);
            _weaponNameLabel.Text = $"[{UIStyle.RarityName(weapon.Rarity)}] {weapon.DisplayName}";
            _weaponNameLabel.AddThemeColorOverride("font_color", rc);

            string typeName = weapon.Type switch
            {
                WeaponType.AutoRifle => "步枪",
                WeaponType.Shotgun => "霰弹",
                WeaponType.HandCannon => "手炮",
                _ => ""
            };
            _weaponStatLabel.Text = $"{typeName} | 伤害:{weapon.BaseDamage} | 射速:{weapon.FireRate:F1}/s";

            if (weapon.Perks.Count > 0)
            {
                var names = new string[weapon.Perks.Count];
                for (int i = 0; i < weapon.Perks.Count; i++)
                    names[i] = PerkSystem.PerkInfo.ContainsKey(weapon.Perks[i]) ? PerkSystem.PerkInfo[weapon.Perks[i]].Name : "?";
                _perksLabel.Text = string.Join(" | ", names);
                _perksLabel.AddThemeColorOverride("font_color", UIStyle.AccentCyan);
            }
            else { _perksLabel.Text = ""; }
        }
        else
        {
            _weaponNameLabel.Text = "无武器";
            _weaponNameLabel.AddThemeColorOverride("font_color", UIStyle.TextMuted);
            _weaponStatLabel.Text = "";
            _perksLabel.Text = "";
        }

        var rg = GetTree().CurrentScene?.GetNodeOrNull<RoomGenerator>("RoomGenerator");
        if (rg != null) _roomLabel.Text = $"房间 {rg.CurrentRoom + 1}/{rg.TotalRooms}";
        _killLabel.Text = $"击杀: {_player.KillCount}";

        // 光等显示
        int totalLight = LightLevelCalculator.CalculateTotalLightLevel(
            _player.Equipment?.CurrentWeapon?.Data, _player.Armors);
        _lightLevelLabel.Text = $"光等: {totalLight}";
    }

    private void UpdateSkillSlot(Control slot, float timer)
    {
        if (slot == null) return;
        var mask = slot.GetNode<ColorRect>("CooldownMask");
        var status = slot.GetNode<Label>("StatusLabel");

        if (timer > 0)
        {
            float ratio = timer / 8.0f;
            mask.Size = new Vector2(46, 46 * Mathf.Clamp(ratio, 0, 1));
            status.Text = $"{timer:F1}s";
            status.AddThemeColorOverride("font_color", UIStyle.TextMuted);
        }
        else
        {
            mask.Size = new Vector2(46, 0);
            status.Text = "就绪";
            status.AddThemeColorOverride("font_color", UIStyle.AccentGreen);
        }
    }

    private void OnHealthChanged(int current, int max)
    {
        _healthBar.MaxValue = max;
        _healthBar.Value = current;
        _hpLabel.Text = $"{current}/{max}";

        float ratio = max > 0 ? (float)current / max : 0;
        if (ratio > 0.6f) UIStyle.UpdateBarColor(_healthBar, UIStyle.HealthGreen);
        else if (ratio > 0.3f) UIStyle.UpdateBarColor(_healthBar, UIStyle.HealthYellow);
        else UIStyle.UpdateBarColor(_healthBar, UIStyle.HealthRed);
    }

    private void OnShieldChanged(float current, float max)
    {
        _shieldBar.MaxValue = max;
        _shieldBar.Value = current;
    }

    private void OnSuperChargeChanged(float current, float max)
    {
        _superBar.MaxValue = max;
        _superBar.Value = current;
    }
}
