using Godot;
using System.Collections.Generic;
using Miao.System;

namespace Miao.UI;

public partial class MainMenu : Control
{
	private Control _currentOverlay;
	private EquipmentScreen _equipmentScreen;

	public override void _Ready()
	{
		// 背景
		var bg = new ColorRect();
		bg.Size = new Vector2(1280, 720);
		bg.Color = UIStyle.BgDark;
		AddChild(bg);

		// 装饰线条
		var line1 = new ColorRect();
		line1.Size = new Vector2(1, 720);
		line1.Position = new Vector2(400, 0);
		line1.Color = new Color(UIStyle.AccentBlue.R, UIStyle.AccentBlue.G, UIStyle.AccentBlue.B, 0.1f);
		AddChild(line1);

		var line2 = new ColorRect();
		line2.Size = new Vector2(1, 720);
		line2.Position = new Vector2(880, 0);
		line2.Color = new Color(UIStyle.AccentBlue.R, UIStyle.AccentBlue.G, UIStyle.AccentBlue.B, 0.1f);
		AddChild(line2);

		// 主容器
		var vbox = new VBoxContainer();
		vbox.Alignment = BoxContainer.AlignmentMode.Center;
		vbox.Position = new Vector2(440, 120);
		vbox.Size = new Vector2(400, 500);
		vbox.AddThemeConstantOverride("separation", 10);
		AddChild(vbox);

		// 标题
		var title = UIStyle.MakeLabel("MIAO", 56, UIStyle.AccentBlue, true);
		title.HorizontalAlignment = HorizontalAlignment.Center;
		vbox.AddChild(title);

		var subtitle = UIStyle.MakeLabel("命运 · 像素", 18, UIStyle.TextMuted);
		subtitle.HorizontalAlignment = HorizontalAlignment.Center;
		vbox.AddChild(subtitle);

		// 分隔线
		var sep = UIStyle.Separator(Vector2.Zero, 200, new Color(UIStyle.AccentBlue.R, UIStyle.AccentBlue.G, UIStyle.AccentBlue.B, 0.3f));
		sep.Size = new Vector2(200, 1);
		var sepContainer = new CenterContainer();
		sepContainer.AddChild(sep);
		vbox.AddChild(sepContainer);

		// 职业选择
		var classLabel = UIStyle.MakeLabel("选择职业", 14, UIStyle.TextSecondary);
		classLabel.HorizontalAlignment = HorizontalAlignment.Center;
		vbox.AddChild(classLabel);

		var hunterBtn = UIStyle.MakeButton("猎人（敏捷远程）", new Vector2(280, 42));
		hunterBtn.Pressed += () => StartWithClass("hunter");
		vbox.AddChild(hunterBtn);

		var titanBtn = UIStyle.MakeButton("泰坦（近战坦克）", new Vector2(280, 42));
		titanBtn.Disabled = true;
		titanBtn.AddThemeColorOverride("font_color", UIStyle.TextMuted);
		vbox.AddChild(titanBtn);

		var warlockBtn = UIStyle.MakeButton("术士（AOE法师）", new Vector2(280, 42));
		warlockBtn.Disabled = true;
		warlockBtn.AddThemeColorOverride("font_color", UIStyle.TextMuted);
		vbox.AddChild(warlockBtn);

		// 分隔
		var spacer = new Control();
		spacer.CustomMinimumSize = new Vector2(0, 8);
		vbox.AddChild(spacer);

		// 出战按钮
		var battleBtn = UIStyle.MakeButton("出战  装备配置", new Vector2(280, 42));
		battleBtn.Pressed += () => OpenEquipmentScreen();
		vbox.AddChild(battleBtn);

		// 功能按钮
		var metaBtn = UIStyle.MakeButton("⚙  Meta 升级", new Vector2(280, 38));
		metaBtn.Pressed += () => ShowMetaUpgradeUI();
		vbox.AddChild(metaBtn);

		var codexBtn = UIStyle.MakeButton("📖  收藏图鉴", new Vector2(280, 38));
		codexBtn.Pressed += () => ShowCodexUI();
		vbox.AddChild(codexBtn);

		var quitBtn = UIStyle.MakeButton("退出", new Vector2(280, 38));
		quitBtn.Pressed += () => GetTree().Quit();
		vbox.AddChild(quitBtn);

		// 版本号
		var version = UIStyle.MakeLabel("v0.1.0", 11, UIStyle.TextMuted);
		version.Position = new Vector2(1220, 698);
		AddChild(version);

		// Equipment screen (hidden by default)
		_equipmentScreen = new EquipmentScreen();
		AddChild(_equipmentScreen);
	}

	private void StartWithClass(string classId)
	{
		// Route through equipment screen before starting
		OpenEquipmentScreen();
	}

	private void OpenEquipmentScreen()
	{
		CloseOverlay();
		_equipmentScreen.Open();
	}

	private void CloseOverlay()
	{
		if (_currentOverlay != null)
		{
			_currentOverlay.QueueFree();
			_currentOverlay = null;
		}
	}

	// ==================== Meta 升级 ====================
	private void ShowMetaUpgradeUI()
	{
		CloseOverlay();
		var meta = MetaProgression.Instance;
		if (meta == null) return;

		var root = new Control();
		root.Size = new Vector2(1280, 720);
		_currentOverlay = root;
		AddChild(root);

		root.AddChild(UIStyle.Overlay(0.92f));

		// 标题
		var title = UIStyle.MakeLabel("⚙ Meta 升级", 28, UIStyle.AccentGold, true);
		title.Position = new Vector2(80, 30);
		root.AddChild(title);

		// 微光余额
		var glimmerLabel = UIStyle.MakeLabel($"微光: {meta.Glimmer}", 18, UIStyle.AccentGold);
		glimmerLabel.Position = new Vector2(80, 70);
		root.AddChild(glimmerLabel);

		// 升级列表
		var upgrades = new (string id, string name, string desc, int current, int max)[]
		{
			("base_health", "基础生命", "每级 +10 最大生命", meta.BaseHealthLevel, 10),
			("base_damage", "基础伤害", "每级 +5% 伤害", meta.BaseDamageLevel, 10),
			("move_speed", "移动速度", "每级 +3% 移速", meta.MoveSpeedLevel, 10),
			("drop_rate", "掉率提升", "每级 +5% 掉率", meta.DropRateLevel, 10),
		};

		int[] costs = { 100, 200, 400, 800, 1600, 3200, 6400, 12800, 25600, 51200 };

		float startY = 120;
		for (int i = 0; i < upgrades.Length; i++)
		{
			float y = startY + i * 100;
			var u = upgrades[i];

			// 面板
			var panel = UIStyle.Panel(new Vector2(1120, 80));
			panel.Position = new Vector2(80, y);
			root.AddChild(panel);

			// 名称
			var nameLabel = UIStyle.MakeLabel(u.name, 18, UIStyle.TextPrimary, true);
			nameLabel.Position = new Vector2(12, 6);
			panel.AddChild(nameLabel);

			// 描述
			var descLabel = UIStyle.MakeLabel(u.desc, 13, UIStyle.TextMuted);
			descLabel.Position = new Vector2(12, 32);
			panel.AddChild(descLabel);

			// 等级进度条
			var levelBar = UIStyle.Bar(new Vector2(12, 54), new Vector2(200, 10), UIStyle.AccentBlue);
			levelBar.MaxValue = u.max;
			levelBar.Value = u.current;
			panel.AddChild(levelBar);

			var levelText = UIStyle.MakeLabel($"Lv.{u.current}/{u.max}", 13, UIStyle.TextSecondary);
			levelText.Position = new Vector2(220, 50);
			panel.AddChild(levelText);

			// 费用和按钮
			bool canAfford = u.current < u.max;
			if (canAfford)
			{
				int cost = u.current < costs.Length ? costs[u.current] : 99999;
				var costLabel = UIStyle.MakeLabel($"费用: {cost}", 14, UIStyle.AccentGold);
				costLabel.Position = new Vector2(800, 12);
				panel.AddChild(costLabel);

				var btn = UIStyle.MakeButton("升级", new Vector2(100, 36));
				btn.Position = new Vector2(960, 16);
				btn.Disabled = meta.Glimmer < cost;
				int idx = i;
				btn.Pressed += () =>
				{
					if (MetaProgression.Instance.TryUpgrade(upgrades[idx].id, upgrades[idx].max))
					{
						CloseOverlay();
						ShowMetaUpgradeUI();
					}
				};
				panel.AddChild(btn);
			}
			else
			{
				var maxLabel = UIStyle.MakeLabel("MAX", 16, UIStyle.AccentGold, true);
				maxLabel.Position = new Vector2(960, 12);
				panel.AddChild(maxLabel);
			}
		}

		// 返回
		var backBtn = UIStyle.MakeButton("返回", new Vector2(120, 36));
		backBtn.Position = new Vector2(80, startY + upgrades.Length * 100 + 20);
		backBtn.Pressed += () => CloseOverlay();
		root.AddChild(backBtn);
	}

	// ==================== 收藏图鉴 ====================
	private void ShowCodexUI()
	{
		CloseOverlay();
		var codex = CollectionCodex.Instance;

		var root = new Control();
		root.Size = new Vector2(1280, 720);
		_currentOverlay = root;
		AddChild(root);

		root.AddChild(UIStyle.Overlay(0.92f));

		var title = UIStyle.MakeLabel("📖 收藏图鉴", 28, UIStyle.AccentCyan, true);
		title.Position = new Vector2(80, 30);
		root.AddChild(title);

		if (codex == null)
		{
			var noData = UIStyle.MakeLabel("暂无数据（进入游戏后解锁）", 16, UIStyle.TextMuted);
			noData.Position = new Vector2(80, 80);
			root.AddChild(noData);

			var backBtn = UIStyle.MakeButton("返回", new Vector2(120, 36));
			backBtn.Position = new Vector2(80, 120);
			backBtn.Pressed += () => CloseOverlay();
			root.AddChild(backBtn);
			return;
		}

		// 总览面板
		var summaryPanel = UIStyle.Panel(new Vector2(1120, 50), UIStyle.AccentGreen);
		summaryPanel.Position = new Vector2(80, 72);
		root.AddChild(summaryPanel);

		var summaryText = UIStyle.MakeLabel(
			$"已解锁: {codex.TotalDiscoveries}  |  全局伤害加成: +{(codex.CollectionDamageBonus - 1) * 100:F0}%",
			15, UIStyle.AccentGreen);
		summaryText.Position = new Vector2(8, 4);
		summaryPanel.AddChild(summaryText);

		float y = 140;
		y = AddCodexSection(root, y, "武器", codex.DiscoveredWeapons);
		var perkNames = new HashSet<string>();
		foreach (var p in codex.DiscoveredPerks)
			if (PerkSystem.PerkInfo.ContainsKey(p)) perkNames.Add(PerkSystem.PerkInfo[p].Name);
		y = AddCodexSection(root, y, "特性", perkNames);
		y = AddCodexSection(root, y, "敌人", codex.DiscoveredEnemies);

		var back = UIStyle.MakeButton("返回", new Vector2(120, 36));
		back.Position = new Vector2(80, y + 20);
		back.Pressed += () => CloseOverlay();
		root.AddChild(back);
	}

	private float AddCodexSection(Control parent, float y, string title, HashSet<string> items)
	{
		var panel = UIStyle.Panel(new Vector2(1120, Mathf.Max(50, 28 + items.Count * 24)));
		panel.Position = new Vector2(80, y);
		parent.AddChild(panel);

		var sectionTitle = UIStyle.MakeLabel($"【{title}】({items.Count})", 15, UIStyle.AccentGold, true);
		sectionTitle.Position = new Vector2(8, 4);
		panel.AddChild(sectionTitle);

		if (items.Count == 0)
		{
			var empty = UIStyle.MakeLabel("暂无", 13, UIStyle.TextMuted);
			empty.Position = new Vector2(24, 28);
			panel.AddChild(empty);
		}
		else
		{
			int idx = 0;
			foreach (var item in items)
			{
				var itemLabel = UIStyle.MakeLabel($"• {item}", 13, UIStyle.TextSecondary);
				itemLabel.Position = new Vector2(24, 28 + idx * 24);
				panel.AddChild(itemLabel);
				idx++;
			}
		}

		return y + panel.Size.Y + 12;
	}
}
