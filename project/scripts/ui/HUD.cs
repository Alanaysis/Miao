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
    private Label _skill1Label;
    private Label _skill2Label;
    private Label _superLabel;
    private Label _roomLabel;
    private Label _killLabel;
    private ColorRect _hpBarFill;
    private Player.Player _player;

    public override void _Ready()
    {
        // === 左上：血条 ===
        _healthBar = CreateBar(new Vector2(20, 20), new Vector2(220, 22), new Color(0.2f, 0.6f, 0.2f));
        _hpLabel = CreateLabel(new Vector2(248, 20), 14);
        _shieldBar = CreateBar(new Vector2(20, 46), new Vector2(220, 10), Colors.Cyan);

        // 血条背景色
        var hpBg = new StyleBoxFlat();
        hpBg.BgColor = new Color(0.15f, 0.15f, 0.15f, 0.8f);
        hpBg.CornerRadiusTopLeft = 4; hpBg.CornerRadiusTopRight = 4;
        hpBg.CornerRadiusBottomLeft = 4; hpBg.CornerRadiusBottomRight = 4;
        _healthBar.AddThemeStyleboxOverride("background", hpBg);

        // === 左下：技能 + 超能 ===
        _skill1Label = CreateLabel(new Vector2(20, 650), 15);
        _skill2Label = CreateLabel(new Vector2(90, 650), 15);
        _superBar = CreateBar(new Vector2(20, 680), new Vector2(160, 14), new Color(1, 0.8f, 0));
        _superLabel = CreateLabel(new Vector2(188, 678), 13);

        // 超能条背景
        var superBg = new StyleBoxFlat();
        superBg.BgColor = new Color(0.15f, 0.15f, 0.1f, 0.8f);
        superBg.CornerRadiusTopLeft = 3; superBg.CornerRadiusTopRight = 3;
        superBg.CornerRadiusBottomLeft = 3; superBg.CornerRadiusBottomRight = 3;
        _superBar.AddThemeStyleboxOverride("background", superBg);

        // === 右下：武器信息面板 ===
        float wpX = 1000, wpY = 620;
        var wpBg = new ColorRect();
        wpBg.Position = new Vector2(wpX - 10, wpY - 5);
        wpBg.Size = new Vector2(270, 90);
        wpBg.Color = new Color(0, 0, 0, 0.5f);
        AddChild(wpBg);

        _weaponNameLabel = CreateLabel(new Vector2(wpX, wpY), 16);
        _weaponStatLabel = CreateLabel(new Vector2(wpX, wpY + 24), 13);
        _perksLabel = CreateLabel(new Vector2(wpX, wpY + 44), 12);

        // === 右上：房间/击杀信息 ===
        _roomLabel = CreateLabel(new Vector2(1100, 20), 15);
        _killLabel = CreateLabel(new Vector2(1100, 42), 13);

        // 自动查找玩家
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

    public override void _Process(double delta)
    {
        if (_player == null) return;

        // 技能冷却
        _skill1Label.Text = _player.Skill1Timer > 0
            ? $"[Q] {_player.Skill1Timer:F1}s"
            : "[Q] 就绪";
        _skill1Label.AddThemeColorOverride("font_color",
            _player.Skill1Timer > 0 ? new Color(0.5f, 0.5f, 0.5f) : new Color(0.4f, 0.9f, 0.4f));

        _skill2Label.Text = _player.Skill2Timer > 0
            ? $"[R] {_player.Skill2Timer:F1}s"
            : "[R] 就绪";
        _skill2Label.AddThemeColorOverride("font_color",
            _player.Skill2Timer > 0 ? new Color(0.5f, 0.5f, 0.5f) : new Color(0.4f, 0.9f, 0.4f));

        _superLabel.Text = _player.IsSuperReady
            ? "[F] 超能就绪！"
            : $"[F] {_player.SuperCharge:F0}/{_player.SuperMaxCharge:F0}";
        _superLabel.AddThemeColorOverride("font_color",
            _player.IsSuperReady ? new Color(1, 0.9f, 0.3f) : new Color(0.7f, 0.6f, 0.3f));

        // 武器信息
        var weapon = _player.Equipment?.CurrentWeapon?.Data;
        if (weapon != null)
        {
            var rc = weapon.Rarity switch
            {
                Rarity.Common => new Color(0.7f, 0.7f, 0.7f),
                Rarity.Uncommon => new Color(0.2f, 0.8f, 0.2f),
                Rarity.Rare => new Color(0.3f, 0.5f, 1.0f),
                Rarity.Epic => new Color(0.7f, 0.3f, 0.9f),
                Rarity.Legendary => new Color(1.0f, 0.85f, 0.1f),
                _ => Colors.White
            };
            _weaponNameLabel.Text = weapon.DisplayName;
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
                _perksLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.85f, 1.0f));
            }
            else
            {
                _perksLabel.Text = "";
            }
        }
        else
        {
            _weaponNameLabel.Text = "无武器";
            _weaponNameLabel.AddThemeColorOverride("font_color", Colors.Gray);
            _weaponStatLabel.Text = "";
            _perksLabel.Text = "";
        }

        // 房间信息
        var rg = GetTree().CurrentScene?.GetNodeOrNull<RoomGenerator>("RoomGenerator");
        if (rg != null)
        {
            _roomLabel.Text = $"房间 {rg.CurrentRoom + 1}/{rg.TotalRooms}";
        }
        _killLabel.Text = $"击杀: {_player.KillCount}";
    }

    private void OnHealthChanged(int current, int max)
    {
        _healthBar.MaxValue = max;
        _healthBar.Value = current;
        _hpLabel.Text = $"{current}/{max}";

        // 血条颜色：绿 → 黄 → 红
        float ratio = max > 0 ? (float)current / max : 0;
        Color color;
        if (ratio > 0.6f) color = new Color(0.2f, 0.7f, 0.2f);
        else if (ratio > 0.3f) color = new Color(0.9f, 0.8f, 0.1f);
        else color = new Color(0.9f, 0.15f, 0.1f);

        var fill = new StyleBoxFlat();
        fill.BgColor = color;
        fill.CornerRadiusTopLeft = 4; fill.CornerRadiusTopRight = 4;
        fill.CornerRadiusBottomLeft = 4; fill.CornerRadiusBottomRight = 4;
        _healthBar.AddThemeStyleboxOverride("fill", fill);
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

    private ProgressBar CreateBar(Vector2 pos, Vector2 size, Color color)
    {
        var bar = new ProgressBar();
        bar.Position = pos;
        bar.Size = size;
        bar.MaxValue = 100;
        bar.Value = 100;
        bar.ShowPercentage = false;

        var fill = new StyleBoxFlat();
        fill.BgColor = color;
        fill.CornerRadiusTopLeft = 4; fill.CornerRadiusTopRight = 4;
        fill.CornerRadiusBottomLeft = 4; fill.CornerRadiusBottomRight = 4;
        bar.AddThemeStyleboxOverride("fill", fill);

        AddChild(bar);
        return bar;
    }

    private Label CreateLabel(Vector2 pos, int fontSize)
    {
        var label = new Label();
        label.Position = pos;
        label.AddThemeFontSizeOverride("font_size", fontSize);
        AddChild(label);
        return label;
    }
}
