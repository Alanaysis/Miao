using Godot;
using Miao.System;
using Miao.Weapon;

namespace Miao.UI;

/// <summary>
/// 统一结算界面 — 胜利/失败共用
/// 显示: 运行统计、微光收益、记忆水晶列表、解码接口、解码结果
/// </summary>
public partial class SettlementUI : CanvasLayer
{
    private Label _title;
    private Label _statsLabel;
    private Label _glimmerLabel;
    private VBoxContainer _engramList;
    private VBoxContainer _resultList;
    private Label _countLabel;
    private Button _decodeAllBtn;
    private Button _restartBtn;
    private Button _menuBtn;

    private EngramDecoder _decoder;
    private int _glimmerEarned;
    private bool _isVictory;
    private string _weaponName;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;

        // 全屏遮罩
        AddChild(UIStyle.Overlay(0.85f));

        // 主面板
        float panelW = 900, panelH = 600;
        float panelX = (1280 - panelW) / 2;
        float panelY = (720 - panelH) / 2;
        var borderColor = _isVictory ? UIStyle.AccentGold : UIStyle.AccentRed;
        var panel = UIStyle.Panel(new Vector2(panelW, panelH), borderColor);
        panel.Position = new Vector2(panelX, panelY);
        AddChild(panel);

        // 标题
        _title = UIStyle.MakeLabel(_isVictory ? "任务完成" : "任务失败", 28, borderColor, true);
        _title.Position = new Vector2((panelW - 200) / 2, 8);
        panel.AddChild(_title);

        // 统计
        _statsLabel = UIStyle.MakeLabel("", 16, UIStyle.TextSecondary);
        _statsLabel.Position = new Vector2(12, 48);
        panel.AddChild(_statsLabel);

        _glimmerLabel = UIStyle.MakeLabel("", 18, UIStyle.AccentGold, true);
        _glimmerLabel.Position = new Vector2(12, 74);
        panel.AddChild(_glimmerLabel);

        // 分隔线
        panel.AddChild(UIStyle.Separator(new Vector2(12, 102), panelW - 24));

        // 水晶计数
        _countLabel = UIStyle.MakeLabel("已收集: 0  |  已解码: 0", 14, UIStyle.TextMuted);
        _countLabel.Position = new Vector2(12, 110);
        panel.AddChild(_countLabel);

        // 左右布局
        float hboxY = 134;
        float hboxH = panelH - hboxY - 60;

        // 左侧: 记忆水晶列表
        float leftW = 400;
        var leftPanel = UIStyle.Panel(new Vector2(leftW, hboxH), UIStyle.AccentCyan);
        leftPanel.Position = new Vector2(12, hboxY);
        panel.AddChild(leftPanel);

        var leftTitle = UIStyle.MakeLabel("记忆水晶", 16, UIStyle.AccentCyan, true);
        leftTitle.Position = new Vector2(8, 2);
        leftPanel.AddChild(leftTitle);

        _engramList = new VBoxContainer();
        _engramList.Position = new Vector2(8, 28);
        _engramList.Size = new Vector2(leftW - 16, hboxH - 74);
        leftPanel.AddChild(_engramList);

        _decodeAllBtn = UIStyle.MakeButton("一键全部解码", new Vector2(200, 32));
        _decodeAllBtn.Position = new Vector2((leftW - 200) / 2, hboxH - 40);
        _decodeAllBtn.Pressed += OnDecodeAll;
        leftPanel.AddChild(_decodeAllBtn);

        // 右侧: 解码结果
        float rightW = panelW - leftW - 40;
        var rightPanel = UIStyle.Panel(new Vector2(rightW, hboxH), UIStyle.AccentGold);
        rightPanel.Position = new Vector2(leftW + 28, hboxY);
        panel.AddChild(rightPanel);

        var rightTitle = UIStyle.MakeLabel("解码结果", 16, UIStyle.AccentGold, true);
        rightTitle.Position = new Vector2(8, 2);
        rightPanel.AddChild(rightTitle);

        _resultList = new VBoxContainer();
        _resultList.Position = new Vector2(8, 28);
        _resultList.Size = new Vector2(rightW - 16, hboxH - 36);
        rightPanel.AddChild(_resultList);

        // 底部分隔线
        panel.AddChild(UIStyle.Separator(new Vector2(12, panelH - 56), panelW - 24));

        // 按钮
        float btnW = 160, btnGap = 30;
        float btnStartX = (panelW - btnW * 2 - btnGap) / 2;
        float btnY = panelH - 48;

        _restartBtn = UIStyle.MakeButton("再来一局", new Vector2(btnW, 36));
        _restartBtn.Position = new Vector2(btnStartX, btnY);
        _restartBtn.ProcessMode = Node.ProcessModeEnum.Always;
        _restartBtn.Pressed += OnRestart;
        panel.AddChild(_restartBtn);

        _menuBtn = UIStyle.MakeButton("返回大厅", new Vector2(btnW, 36));
        _menuBtn.Position = new Vector2(btnStartX + btnW + btnGap, btnY);
        _menuBtn.ProcessMode = Node.ProcessModeEnum.Always;
        _menuBtn.Pressed += OnReturnToMenu;
        panel.AddChild(_menuBtn);

        Hide();
    }

    /// <summary>
    /// 设置结算数据并显示界面
    /// </summary>
    public void SetData(bool isVictory, int kills, float time, int glimmerEarned,
        EngramDecoder decoder, string weaponName)
    {
        _isVictory = isVictory;
        _glimmerEarned = glimmerEarned;
        _decoder = decoder;
        _weaponName = weaponName;

        int minutes = (int)(time / 60);
        int seconds = (int)(time % 60);

        if (_title != null)
        {
            var borderColor = _isVictory ? UIStyle.AccentGold : UIStyle.AccentRed;
            _title.Text = _isVictory ? "任务完成" : "任务失败";
            _title.AddThemeColorOverride("font_color", borderColor);
        }

        _statsLabel.Text = $"击杀: {kills} | 用时: {minutes}:{seconds:D2} | 通关武器: {weaponName}";
        _glimmerLabel.Text = $"获得微光: {glimmerEarned}";

        RefreshEngramList();
        Show();
    }

    private void RefreshEngramList()
    {
        // 清空旧内容
        foreach (var child in _engramList.GetChildren()) child.QueueFree();
        foreach (var child in _resultList.GetChildren()) child.QueueFree();

        if (_decoder == null) return;

        // 更新计数
        int total = _decoder.CollectedEngrams.Count;
        int decoded = _decoder.DecodedWeapons.Count;
        _countLabel.Text = $"已收集: {total}  |  已解码: {decoded}";

        // 水晶列表（按钮形式，可逐个解码）
        for (int i = 0; i < _decoder.CollectedEngrams.Count; i++)
        {
            var engram = _decoder.CollectedEngrams[i];
            bool isDecoded = engram.WeaponData != null && _decoder.DecodedWeapons.Contains(engram.WeaponData);

            var rc = UIStyle.RarityColor(engram.Rarity);
            var btn = UIStyle.MakeButton(
                isDecoded ? $"[已解码] {UIStyle.RarityName(engram.Rarity)} 水晶"
                          : $"[{UIStyle.RarityName(engram.Rarity)}] 记忆水晶",
                new Vector2(360, 28)
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

        if (_decoder.CollectedEngrams.Count == 0)
        {
            _engramList.AddChild(UIStyle.MakeLabel("未收集到记忆水晶", 14, UIStyle.TextMuted));
        }

        // 解码结果列表
        foreach (var weapon in _decoder.DecodedWeapons)
        {
            var rc = UIStyle.RarityColor(weapon.Rarity);
            var label = UIStyle.MakeLabel(
                $"[{UIStyle.RarityName(weapon.Rarity)}] {weapon.DisplayName}",
                14, rc
            );
            _resultList.AddChild(label);
        }

        if (_decoder.DecodedWeapons.Count == 0)
        {
            _resultList.AddChild(UIStyle.MakeLabel("尚未解码任何水晶", 14, UIStyle.TextMuted));
        }

        // 更新一键解码按钮状态
        if (_decodeAllBtn != null)
        {
            _decodeAllBtn.Disabled = _decoder.GetUndecodedCount() == 0;
        }
    }

    private void OnDecodeSingle(int index)
    {
        var weapon = _decoder?.DecodeSingle(index);
        if (weapon != null)
        {
            GD.Print($"解码获得: [{weapon.Rarity}] {weapon.DisplayName}");
        }
        RefreshEngramList();
    }

    private void OnDecodeAll()
    {
        _decoder?.DecodeAll();
        GD.Print($"一键解码完成，共获得 {_decoder?.DecodedWeapons.Count ?? 0} 把武器");
        RefreshEngramList();
    }

    private void OnRestart()
    {
        QueueFree();
        GameManager.Instance.StartGame();
    }

    private void OnReturnToMenu()
    {
        QueueFree();
        GameManager.Instance.ReturnToLobby();
    }
}
