using Godot;
using Miao.System;

namespace Miao.UI;

/// <summary>
/// 主菜单界面
/// 游戏入口，显示开始游戏按钮
/// </summary>
public partial class MainMenu : Control
{
    public override void _Ready()
    {
        // 铺满窗口
        SetAnchorsPreset(Control.LayoutPreset.FullRect);
        CreateUI();
    }

    private void CreateUI()
    {
        // 背景色
        var background = new ColorRect();
        background.Color = new Color(0.1f, 0.1f, 0.15f, 1.0f);
        background.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(background);

        // 游戏标题
        var titleLabel = new Label();
        titleLabel.Text = "Miao";
        titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
        titleLabel.AddThemeFontSizeOverride("font_size", 72);
        titleLabel.Position = new Vector2(0, 200);
        titleLabel.Size = new Vector2(1280, 90);
        AddChild(titleLabel);

        // 副标题
        var subtitleLabel = new Label();
        subtitleLabel.Text = "Roguelike 实验";
        subtitleLabel.HorizontalAlignment = HorizontalAlignment.Center;
        subtitleLabel.AddThemeFontSizeOverride("font_size", 24);
        subtitleLabel.Position = new Vector2(0, 300);
        subtitleLabel.Size = new Vector2(1280, 40);
        AddChild(subtitleLabel);

        // 开始游戏按钮
        var startButton = new Button();
        startButton.Text = "开始游戏";
        startButton.Position = new Vector2(515, 400);
        startButton.Size = new Vector2(250, 60);
        startButton.Pressed += OnStartPressed;
        AddChild(startButton);

        // 退出按钮
        var quitButton = new Button();
        quitButton.Text = "退出";
        quitButton.Position = new Vector2(515, 480);
        quitButton.Size = new Vector2(250, 60);
        quitButton.Pressed += OnQuitPressed;
        AddChild(quitButton);
    }

    private void OnStartPressed()
    {
        GameManager.Instance.StartGame();
    }

    private void OnQuitPressed()
    {
        GetTree().Quit();
    }
}
