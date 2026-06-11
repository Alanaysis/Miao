using Godot;
using Miao.System;
using Miao.Weapon;

namespace Miao.UI;

public partial class CodexUI : CanvasLayer
{
    private Label _statsLabel;
    private VBoxContainer _weaponList;
    private VBoxContainer _perkList;
    private VBoxContainer _enemyList;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;

        var panel = new PanelContainer();
        panel.Size = new Vector2(800, 550);
        panel.Position = new Vector2(240, 85);

        var mainVbox = new VBoxContainer();

        // Title
        var title = UIStyle.MakeLabel("收藏图鉴", 24, UIStyle.AccentGold, true);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        mainVbox.AddChild(title);

        // Stats
        _statsLabel = UIStyle.MakeLabel("", 14, UIStyle.TextSecondary);
        mainVbox.AddChild(_statsLabel);

        mainVbox.AddChild(UIStyle.Separator(Vector2.Zero, 750));

        // Tab container
        var tabs = new TabContainer();
        tabs.CustomMinimumSize = new Vector2(0, 380);

        // Weapons tab
        var weaponVbox = new VBoxContainer();
        weaponVbox.Name = "武器";
        _weaponList = new VBoxContainer();
        weaponVbox.AddChild(_weaponList);
        tabs.AddChild(weaponVbox);

        // Perks tab
        var perkVbox = new VBoxContainer();
        perkVbox.Name = "Perk";
        _perkList = new VBoxContainer();
        perkVbox.AddChild(_perkList);
        tabs.AddChild(perkVbox);

        // Enemies tab
        var enemyVbox = new VBoxContainer();
        enemyVbox.Name = "敌人";
        _enemyList = new VBoxContainer();
        enemyVbox.AddChild(_enemyList);
        tabs.AddChild(enemyVbox);

        mainVbox.AddChild(tabs);

        mainVbox.AddChild(UIStyle.Separator(Vector2.Zero, 750));

        // Close button
        var closeBtn = UIStyle.MakeButton("关闭", new Vector2(120, 35));
        closeBtn.Pressed += () => Hide();
        mainVbox.AddChild(closeBtn);

        panel.AddChild(mainVbox);
        AddChild(panel);

        Hide();
    }

    public void Open()
    {
        RefreshAll();
        Show();
    }

    private void RefreshAll()
    {
        var codex = CollectionCodex.Instance;
        if (codex == null)
        {
            _statsLabel.Text = "图鉴系统未加载";
            return;
        }

        // Stats
        int total = codex.TotalDiscoveries;
        float bonus = codex.CollectionDamageBonus;
        _statsLabel.Text = $"已发现: {total} 项 | 全局伤害加成: +{(bonus - 1) * 100:F0}%";

        // Weapons
        foreach (var child in _weaponList.GetChildren()) child.QueueFree();
        if (codex.DiscoveredWeapons.Count == 0)
        {
            _weaponList.AddChild(UIStyle.MakeLabel("尚未发现任何武器", 14, UIStyle.TextMuted));
        }
        else
        {
            foreach (var weaponKey in codex.DiscoveredWeapons)
            {
                var parts = weaponKey.Split('_');
                string typeName = parts.Length > 0 ? parts[0] : weaponKey;
                string rarityName = parts.Length > 1 ? parts[1] : "";

                var hbox = new HBoxContainer();
                hbox.AddChild(UIStyle.MakeLabel($"• {typeName}", 14));

                bool cleared = codex.WeaponsClearedWith.Contains(weaponKey);
                if (cleared)
                {
                    var badge = UIStyle.MakeLabel(" ✓通关", 12, UIStyle.AccentGreen);
                    hbox.AddChild(badge);
                }

                _weaponList.AddChild(hbox);
            }
        }

        // Perks
        foreach (var child in _perkList.GetChildren()) child.QueueFree();
        if (codex.DiscoveredPerks.Count == 0)
        {
            _perkList.AddChild(UIStyle.MakeLabel("尚未发现任何 Perk", 14, UIStyle.TextMuted));
        }
        else
        {
            foreach (var perk in codex.DiscoveredPerks)
            {
                if (PerkSystem.PerkInfo.TryGetValue(perk, out var info))
                {
                    _perkList.AddChild(UIStyle.MakeLabel($"• {info.Name} — {info.Desc}", 14));
                }
            }
        }

        // Enemies
        foreach (var child in _enemyList.GetChildren()) child.QueueFree();
        if (codex.DiscoveredEnemies.Count == 0)
        {
            _enemyList.AddChild(UIStyle.MakeLabel("尚未发现任何敌人", 14, UIStyle.TextMuted));
        }
        else
        {
            foreach (var enemy in codex.DiscoveredEnemies)
            {
                _enemyList.AddChild(UIStyle.MakeLabel($"• {enemy}", 14));
            }
        }
    }
}
