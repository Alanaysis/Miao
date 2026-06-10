using Godot;
using System.Collections.Generic;
using Miao.System;

namespace Miao.UI;

public partial class MainMenu : Control
{
	private Control _currentOverlay;
	private EquipmentScreen _equipmentScreen;
	private MetaShopUI _metaShopUI;

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
		titanBtn.Pressed += () => StartWithClass("titan");
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
		var metaBtn = UIStyle.MakeButton("🏪  Meta 商店", new Vector2(280, 38));
		metaBtn.Pressed += () => _metaShopUI.Open();
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

		// Meta shop UI (hidden by default)
		_metaShopUI = new MetaShopUI();
		AddChild(_metaShopUI);
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
