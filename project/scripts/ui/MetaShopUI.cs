using Godot;
using global::System.Collections.Generic;
using Miao.System;

namespace Miao.UI;

public partial class MetaShopUI : CanvasLayer
{
    private TabContainer _tabs;
    private Label _glimmerLabel;
    private VBoxContainer _subclassList;
    private VBoxContainer _aspectList;
    private VBoxContainer _modList;
    private VBoxContainer _lightModuleList;
    private VBoxContainer _weaponList;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;

        var panel = new PanelContainer();
        panel.Size = new Vector2(900, 600);
        panel.Position = new Vector2(190, 60);

        var mainVbox = new VBoxContainer();

        // Header
        var headerBox = new HBoxContainer();
        headerBox.AddChild(UIStyle.MakeLabel("Meta 商店", 24));
        _glimmerLabel = UIStyle.MakeLabel("微光: 0", 18);
        _glimmerLabel.HorizontalAlignment = HorizontalAlignment.Right;
        headerBox.AddChild(_glimmerLabel);
        mainVbox.AddChild(headerBox);

        // Separator
        var sep = new HSeparator();
        mainVbox.AddChild(sep);

        // Tab container for categories
        _tabs = new TabContainer();
        _tabs.CustomMinimumSize = new Vector2(0, 450);

        // Subclass tab
        _subclassList = new VBoxContainer();
        _subclassList.Name = "子类";
        _tabs.AddChild(_subclassList);

        // Aspect tab
        _aspectList = new VBoxContainer();
        _aspectList.Name = "星相";
        _tabs.AddChild(_aspectList);

        // Mod tab
        _modList = new VBoxContainer();
        _modList.Name = "Mod";
        _tabs.AddChild(_modList);

        // Light Level Module tab
        _lightModuleList = new VBoxContainer();
        _lightModuleList.Name = "光等模块";
        _tabs.AddChild(_lightModuleList);

        // Weapon tab
        _weaponList = new VBoxContainer();
        _weaponList.Name = "武器";
        _tabs.AddChild(_weaponList);

        mainVbox.AddChild(_tabs);

        // Close button
        var closeBtn = new Button();
        closeBtn.Text = "关闭";
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
        var meta = MetaProgression.Instance;
        _glimmerLabel.Text = $"微光: {meta.Glimmer}";

        RefreshSubclassTab();
        RefreshModTab();
        RefreshLightModuleTab();
    }

    private void RefreshSubclassTab()
    {
        foreach (var child in _subclassList.GetChildren()) child.QueueFree();

        var meta = MetaProgression.Instance;

        // Hunter subclasses
        _subclassList.AddChild(UIStyle.MakeLabel("猎人子类", 16));
        AddSubclassItem(_subclassList, "hunter", "arc", "电弧猎人", 1000, meta);
        AddSubclassItem(_subclassList, "hunter", "solar", "烈日猎人", 1000, meta);

        _subclassList.AddChild(new HSeparator());

        // Titan subclasses
        _subclassList.AddChild(UIStyle.MakeLabel("泰坦子类", 16));
        AddSubclassItem(_subclassList, "titan", "solar", "烈日泰坦", 1500, meta);
        AddSubclassItem(_subclassList, "titan", "arc", "电弧泰坦", 1500, meta);
        AddSubclassItem(_subclassList, "titan", "void", "虚空泰坦", 1500, meta);

        _subclassList.AddChild(new HSeparator());

        // Warlock subclasses
        _subclassList.AddChild(UIStyle.MakeLabel("术士子类", 16));
        AddSubclassItem(_subclassList, "warlock", "arc", "电弧术士", 1500, meta);
        AddSubclassItem(_subclassList, "warlock", "void", "虚空术士", 1500, meta);
        AddSubclassItem(_subclassList, "warlock", "solar", "烈日术士", 1500, meta);
    }

    private void AddSubclassItem(VBoxContainer list, string className, string element, string displayName, int cost, MetaProgression meta)
    {
        var hbox = new HBoxContainer();
        hbox.AddChild(UIStyle.MakeLabel(displayName, 14));

        bool unlocked = meta.IsSubclassUnlocked(className, element);
        if (unlocked)
        {
            var label = UIStyle.MakeLabel("已解锁", 14);
            label.Modulate = new Color("#22c55e");
            hbox.AddChild(label);
        }
        else
        {
            var btn = new Button();
            btn.Text = $"解锁 ({cost} 微光)";
            btn.Disabled = meta.Glimmer < cost;
            btn.Pressed += () =>
            {
                if (meta.TryUnlockSubclass(className, element, cost))
                {
                    RefreshAll();
                }
            };
            hbox.AddChild(btn);
        }

        list.AddChild(hbox);
    }

    private void RefreshModTab()
    {
        foreach (var child in _modList.GetChildren()) child.QueueFree();

        var meta = MetaProgression.Instance;
        var modsData = Miao.Data.DataLoader.Load<ModsConfig>("mods.json");

        if (modsData?.Mods == null) return;

        foreach (var mod in modsData.Mods)
        {
            if (mod.Acquisition != "meta_shop") continue;

            var hbox = new HBoxContainer();
            hbox.AddChild(UIStyle.MakeLabel($"{mod.Name} - {mod.Description}", 12));

            bool unlocked = meta.IsModUnlocked(mod.Id);
            if (unlocked)
            {
                var label = UIStyle.MakeLabel("已解锁", 12);
                label.Modulate = new Color("#22c55e");
                hbox.AddChild(label);
            }
            else
            {
                var btn = new Button();
                btn.Text = $"({mod.Cost} 微光)";
                btn.Disabled = meta.Glimmer < mod.Cost;
                btn.Pressed += () =>
                {
                    if (meta.TryUnlockMod(mod.Id, mod.Cost))
                    {
                        RefreshAll();
                    }
                };
                hbox.AddChild(btn);
            }

            _modList.AddChild(hbox);
        }
    }

    private void RefreshLightModuleTab()
    {
        foreach (var child in _lightModuleList.GetChildren()) child.QueueFree();

        var meta = MetaProgression.Instance;

        _lightModuleList.AddChild(UIStyle.MakeLabel("光等升级模块", 16));
        _lightModuleList.AddChild(UIStyle.MakeLabel("每个模块永久提升所有装备的基础光等", 12));

        for (int level = 1; level <= 10; level++)
        {
            int cost = level * 500;
            var hbox = new HBoxContainer();
            hbox.AddChild(UIStyle.MakeLabel($"光等模块 Lv.{level} (+{level * 2} 光等)", 14));

            bool purchased = meta.GetLightModuleLevel() >= level;
            if (purchased)
            {
                var label = UIStyle.MakeLabel("已购买", 14);
                label.Modulate = new Color("#22c55e");
                hbox.AddChild(label);
            }
            else
            {
                var btn = new Button();
                btn.Text = $"({cost} 微光)";
                btn.Disabled = meta.Glimmer < cost || meta.GetLightModuleLevel() < level - 1;
                btn.Pressed += () =>
                {
                    if (meta.TryBuyLightModule(cost))
                    {
                        RefreshAll();
                    }
                };
                hbox.AddChild(btn);
            }

            _lightModuleList.AddChild(hbox);
        }
    }
}

public class ModsConfig
{
    [global::System.Text.Json.Serialization.JsonPropertyName("mods")]
    public List<Miao.Armor.ModData> Mods { get; set; }
}
