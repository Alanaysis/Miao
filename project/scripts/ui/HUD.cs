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
    private Label _weaponLabel;
    private Label _perksLabel;
    private Label _skill1Label;
    private Label _skill2Label;
    private Label _superLabel;
    private Player _player;

    public override void _Ready()
    {
        // 光等条（左上）
        _healthBar = CreateBar(new Vector2(20, 20), new Vector2(200, 20), Colors.Red);
        _shieldBar = CreateBar(new Vector2(20, 45), new Vector2(200, 12), Colors.Cyan);

        // 技能图标 + 超能能量（左下）
        _skill1Label = CreateLabel(new Vector2(20, 660), 16);
        _skill2Label = CreateLabel(new Vector2(80, 660), 16);
        _superBar = CreateBar(new Vector2(20, 690), new Vector2(140, 12), new Color(1, 0.8f, 0));
        _superLabel = CreateLabel(new Vector2(170, 688), 12);

        // 武器信息（右下）
        _weaponLabel = CreateLabel(new Vector2(1050, 660), 14);
        _perksLabel = CreateLabel(new Vector2(1050, 680), 12);

        // 自动查找玩家（兼容旧场景加载方式）
        if (_player == null)
        {
            var node = GetTree().GetFirstNodeInGroup("player");
            if (node is Player p)
                SetPlayer(p);
        }
    }

    public void SetPlayer(Player player)
    {
        _player = player;
        player.HealthChanged += OnHealthChanged;
        player.ShieldChanged += OnShieldChanged;
        player.SuperChargeChanged += OnSuperChargeChanged;
    }

    public override void _Process(double delta)
    {
        if (_player == null) return;

        _skill1Label.Text = _player.Skill1Timer > 0
            ? $"Q: {_player.Skill1Timer:F1}s"
            : "Q: 就绪";
        _skill2Label.Text = _player.Skill2Timer > 0
            ? $"R: {_player.Skill2Timer:F1}s"
            : "R: 就绪";
        _superLabel.Text = _player.IsSuperReady
            ? "F: 超能就绪！"
            : $"F: {_player.SuperCharge:F0}/{_player.SuperMaxCharge:F0}";

        var weapon = _player.Equipment?.CurrentWeapon?.Data;
        if (weapon != null)
        {
            string rarityColor = weapon.Rarity switch
            {
                Rarity.Common => "白",
                Rarity.Uncommon => "绿",
                Rarity.Rare => "蓝",
                Rarity.Epic => "紫",
                Rarity.Legendary => "金",
                _ => ""
            };
            _weaponLabel.Text = $"[{rarityColor}] {weapon.DisplayName}";
            _perksLabel.Text = string.Join(" | ", weapon.Perks.ConvertAll(p => PerkSystem.PerkInfo[p].Name));
        }
        else
        {
            _weaponLabel.Text = "无武器";
            _perksLabel.Text = "";
        }
    }

    private void OnHealthChanged(int current, int max)
    {
        _healthBar.MaxValue = max;
        _healthBar.Value = current;
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
        AddChild(bar);
        return bar;
    }

    private Label CreateLabel(Vector2 pos, int fontSize)
    {
        var label = new Label();
        label.Position = pos;
        AddChild(label);
        return label;
    }
}
