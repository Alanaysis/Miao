using Godot;
using System.Collections.Generic;

namespace Miao.UI;

/// <summary>
/// 升级选项UI
/// 显示3个随机升级选项供玩家选择
/// </summary>
public partial class LevelUpUI : CanvasLayer
{
    /// <summary>升级选项选择信号</summary>
    [Signal]
    public delegate void UpgradeSelectedEventHandler(string upgradeId);

    private static readonly string[] AllUpgrades = {
        "attack_speed",
        "move_speed",
        "heal",
        "max_health",
        "new_weapon"
    };

    private static readonly Dictionary<string, string> UpgradeNames = new()
    {
        { "attack_speed", "攻击速度 +20%" },
        { "move_speed", "移动速度 +15%" },
        { "heal", "恢复 30% 生命" },
        { "max_health", "最大生命 +20" },
        { "new_weapon", "获得新武器" }
    };

    private static readonly Dictionary<string, string> UpgradeDescriptions = new()
    {
        { "attack_speed", "所有武器攻击速度提升20%" },
        { "move_speed", "移动速度提升15%" },
        { "heal", "立即恢复最大生命值的30%" },
        { "max_health", "最大生命值增加20，并恢复20点生命" },
        { "new_weapon", "获得远程射击武器，已有则增加伤害" }
    };

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        CreateUI();
    }

    private void CreateUI()
    {
        // 根容器
        var root = new Control();
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(root);

        // 背景遮罩
        var background = new ColorRect();
        background.Color = new Color(0, 0, 0, 0.7f);
        background.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        root.AddChild(background);

        // 标题
        var titleLabel = new Label();
        titleLabel.Text = "选择升级";
        titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
        titleLabel.AddThemeFontSizeOverride("font_size", 32);
        titleLabel.Position = new Vector2(0, 200);
        titleLabel.Size = new Vector2(1280, 50);
        root.AddChild(titleLabel);

        // 获取3个随机升级选项
        var upgrades = GetRandomUpgrades(3);

        // 创建选项按钮
        float startX = (1280 - upgrades.Count * 280) / 2 + 10;
        for (int i = 0; i < upgrades.Count; i++)
        {
            var upgradeId = upgrades[i];
            CreateUpgradeButton(root, upgradeId, startX + i * 280, 320);
        }
    }

    private void CreateUpgradeButton(Control root, string upgradeId, float x, float y)
    {
        var button = new Button();
        button.Text = $"{UpgradeNames[upgradeId]}\n\n{UpgradeDescriptions[upgradeId]}";
        button.Position = new Vector2(x, y);
        button.Size = new Vector2(250, 120);
        button.AddThemeFontSizeOverride("font_size", 14);
        button.Pressed += () => OnUpgradeButtonPressed(upgradeId);
        root.AddChild(button);
    }

    private void OnUpgradeButtonPressed(string upgradeId)
    {
        EmitSignal(SignalName.UpgradeSelected, upgradeId);
    }

    private List<string> GetRandomUpgrades(int count)
    {
        var available = new List<string>(AllUpgrades);
        var selected = new List<string>();

        for (int i = 0; i < count && available.Count > 0; i++)
        {
            int index = GD.RandRange(0, available.Count - 1);
            selected.Add(available[index]);
            available.RemoveAt(index);
        }

        return selected;
    }
}
