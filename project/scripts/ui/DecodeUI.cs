using Godot;
using System.Collections.Generic;
using Miao.System;
using Miao.Weapon;

namespace Miao.UI;

/// <summary>
/// 记忆水晶解码界面 — 结算时显示，可逐个或一键解码
/// </summary>
public partial class DecodeUI : CanvasLayer
{
    private VBoxContainer _engramList;
    private VBoxContainer _resultList;
    private Button _decodeAllBtn;
    private Label _countLabel;
    private EngramDecoder _decoder;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;

        // 全屏遮罩
        AddChild(UIStyle.Overlay(0.85f));

        // 主面板
        float panelW = 800, panelH = 500;
        float panelX = (1280 - panelW) / 2;
        float panelY = (720 - panelH) / 2;
        var panel = UIStyle.Panel(new Vector2(panelW, panelH), UIStyle.AccentPurple);
        panel.Position = new Vector2(panelX, panelY);
        AddChild(panel);

        // 标题
        var title = UIStyle.MakeLabel("记忆水晶解码", 24, UIStyle.AccentPurple, true);
        title.Position = new Vector2(12, 4);
        panel.AddChild(title);

        // 水晶计数
        _countLabel = UIStyle.MakeLabel("已收集: 0  |  已解码: 0", 14, UIStyle.TextSecondary);
        _countLabel.Position = new Vector2(250, 8);
        panel.AddChild(_countLabel);

        // 分隔线
        panel.AddChild(UIStyle.Separator(new Vector2(12, 40), panelW - 24));

        // 左右布局容器
        var hbox = new HBoxContainer();
        hbox.Position = new Vector2(12, 52);
        hbox.Size = new Vector2(panelW - 24, panelH - 110);
        hbox.AddThemeConstantOverride("separation", 16);
        panel.AddChild(hbox);

        // 左侧: 水晶列表
        var leftVbox = new VBoxContainer();
        leftVbox.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        var leftTitle = UIStyle.MakeLabel("待解码水晶", 16, UIStyle.AccentCyan, true);
        leftVbox.AddChild(leftTitle);

        _engramList = new VBoxContainer();
        _engramList.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        leftVbox.AddChild(_engramList);

        _decodeAllBtn = UIStyle.MakeButton("一键全部解码", new Vector2(200, 36));
        _decodeAllBtn.Pressed += OnDecodeAll;
        leftVbox.AddChild(_decodeAllBtn);

        hbox.AddChild(leftVbox);

        // 分隔线
        var sep = new VSeparator();
        hbox.AddChild(sep);

        // 右侧: 解码结果
        var rightVbox = new VBoxContainer();
        rightVbox.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        var rightTitle = UIStyle.MakeLabel("解码结果", 16, UIStyle.AccentGold, true);
        rightVbox.AddChild(rightTitle);

        _resultList = new VBoxContainer();
        _resultList.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        rightVbox.AddChild(_resultList);

        hbox.AddChild(rightVbox);

        // 底部按钮
        float btnY = panelH - 48;
        var closeBtn = UIStyle.MakeButton("确认", new Vector2(120, 36));
        closeBtn.Position = new Vector2((panelW - 120) / 2, btnY);
        closeBtn.Pressed += () =>
        {
            QueueFree();
            GetTree().Paused = false;
        };
        panel.AddChild(closeBtn);

        Hide();
    }

    public void SetDecoder(EngramDecoder decoder)
    {
        _decoder = decoder;
        RefreshUI();
    }

    private void RefreshUI()
    {
        // Clear
        foreach (var child in _engramList.GetChildren()) child.QueueFree();
        foreach (var child in _resultList.GetChildren()) child.QueueFree();

        if (_decoder == null) return;

        // 更新计数
        int total = _decoder.CollectedEngrams.Count;
        int decoded = _decoder.DecodedWeapons.Count;
        _countLabel.Text = $"已收集: {total}  |  已解码: {decoded}";

        // Show engrams
        for (int i = 0; i < _decoder.CollectedEngrams.Count; i++)
        {
            var engram = _decoder.CollectedEngrams[i];
            bool isDecoded = engram.WeaponData != null && _decoder.DecodedWeapons.Contains(engram.WeaponData);

            var btn = UIStyle.MakeButton(
                isDecoded ? $"[已解码] {engram.Rarity} 水晶" : $"[{engram.Rarity}] 记忆水晶",
                new Vector2(300, 32)
            );

            if (isDecoded)
            {
                btn.Disabled = true;
            }
            else
            {
                int index = i;
                btn.Pressed += () => OnDecodeSingle(index);
            }
            _engramList.AddChild(btn);
        }

        // Show decoded weapons
        foreach (var weapon in _decoder.DecodedWeapons)
        {
            var rc = UIStyle.RarityColor(weapon.Rarity);
            var label = UIStyle.MakeLabel(
                $"[{UIStyle.RarityName(weapon.Rarity)}] {weapon.DisplayName}",
                14, rc
            );
            _resultList.AddChild(label);
        }

        // 无解码结果时显示提示
        if (_decoder.DecodedWeapons.Count == 0)
        {
            _resultList.AddChild(UIStyle.MakeLabel("尚未解码任何水晶", 14, UIStyle.TextMuted));
        }
    }

    private void OnDecodeSingle(int index)
    {
        var weapon = _decoder.DecodeSingle(index);
        if (weapon != null)
        {
            GD.Print($"解码获得: [{weapon.Rarity}] {weapon.DisplayName}");
            RefreshUI();
        }
    }

    private void OnDecodeAll()
    {
        _decoder.DecodeAll();
        GD.Print($"一键解码完成，共获得 {_decoder.DecodedWeapons.Count} 把武器");
        RefreshUI();
    }
}
