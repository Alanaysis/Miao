using Godot;
using Miao.System;

namespace Miao.UI;

/// <summary>
/// 游戏结束界面
/// 显示存活时间、击杀数、重新开始按钮
/// </summary>
public partial class GameOverUI : CanvasLayer
{
    private Label _timeLabel;
    private Label _killLabel;
    private Label _goldLabel;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        CreateUI();
    }

    public void SetStats(float gameTime, int killCount, int goldReward = 0)
    {
        var minutes = (int)(gameTime / 60);
        var seconds = (int)(gameTime % 60);
        if (_timeLabel != null)
            _timeLabel.Text = $"存活时间: {minutes:D2}:{seconds:D2}";
        if (_killLabel != null)
            _killLabel.Text = $"击杀数: {killCount}";
        if (_goldLabel != null)
            _goldLabel.Text = $"获得金币: {goldReward}";
    }

    private void CreateUI()
    {
        // 根容器
        var root = new Control();
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(root);

        // 背景遮罩
        var background = new ColorRect();
        background.Color = new Color(0, 0, 0, 0.8f);
        background.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        root.AddChild(background);

        // 标题
        var titleLabel = new Label();
        titleLabel.Text = "游戏结束";
        titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
        titleLabel.AddThemeFontSizeOverride("font_size", 48);
        titleLabel.Position = new Vector2(0, 180);
        titleLabel.Size = new Vector2(1280, 60);
        root.AddChild(titleLabel);

        // 存活时间
        _timeLabel = new Label();
        _timeLabel.Text = "存活时间: 00:00";
        _timeLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _timeLabel.AddThemeFontSizeOverride("font_size", 24);
        _timeLabel.Position = new Vector2(0, 300);
        _timeLabel.Size = new Vector2(1280, 40);
        root.AddChild(_timeLabel);

        // 击杀数
        _killLabel = new Label();
        _killLabel.Text = "击杀数: 0";
        _killLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _killLabel.AddThemeFontSizeOverride("font_size", 24);
        _killLabel.Position = new Vector2(0, 350);
        _killLabel.Size = new Vector2(1280, 40);
        root.AddChild(_killLabel);

        // 金币奖励
        _goldLabel = new Label();
        _goldLabel.Text = "获得金币: 0";
        _goldLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _goldLabel.AddThemeFontSizeOverride("font_size", 24);
        _goldLabel.Position = new Vector2(0, 400);
        _goldLabel.Size = new Vector2(1280, 40);
        root.AddChild(_goldLabel);

        // 重新开始按钮
        var restartButton = new Button();
        restartButton.Text = "重新开始";
        restartButton.Position = new Vector2(540, 480);
        restartButton.Size = new Vector2(200, 50);
        restartButton.Pressed += OnRestartPressed;
        root.AddChild(restartButton);

        // 返回菜单按钮
        var menuButton = new Button();
        menuButton.Text = "返回菜单";
        menuButton.Position = new Vector2(540, 550);
        menuButton.Size = new Vector2(200, 50);
        menuButton.Pressed += OnMenuPressed;
        root.AddChild(menuButton);
    }

    private void OnRestartPressed()
    {
        GetTree().Paused = false;
        GameManager.Instance.StartGame();
    }

    private void OnMenuPressed()
    {
        GetTree().Paused = false;
        GameManager.Instance.ReturnToMainMenu();
    }
}
