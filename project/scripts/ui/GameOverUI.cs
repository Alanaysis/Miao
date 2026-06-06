using Godot;
using Miao.System;

namespace Miao.UI;

public partial class SettlementUI : CanvasLayer
{
    private Label _statsLabel;
    private Label _glimmerLabel;
    private Label _weaponLabel;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;

        var panel = new PanelContainer();
        panel.Size = new Vector2(600, 400);
        panel.Position = new Vector2(340, 160);

        var vbox = new VBoxContainer();

        var title = new Label();
        title.Text = "任务结算";
        title.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(title);

        _statsLabel = new Label();
        vbox.AddChild(_statsLabel);

        _glimmerLabel = new Label();
        vbox.AddChild(_glimmerLabel);

        _weaponLabel = new Label();
        vbox.AddChild(_weaponLabel);

        var buttonBox = new HBoxContainer();
        buttonBox.Alignment = BoxContainer.AlignmentMode.Center;

        var restartBtn = new Button();
        restartBtn.Text = "再来一局";
        restartBtn.Pressed += () => GameManager.Instance.StartGame();
        buttonBox.AddChild(restartBtn);

        var menuBtn = new Button();
        menuBtn.Text = "返回大厅";
        menuBtn.Pressed += () => GameManager.Instance.ReturnToMainMenu();
        buttonBox.AddChild(menuBtn);

        vbox.AddChild(buttonBox);
        panel.AddChild(vbox);
        AddChild(panel);
    }

    public void SetRunStats(float time, int kills, int glimmerEarned, string weaponName)
    {
        int minutes = (int)(time / 60);
        int seconds = (int)(time % 60);
        _statsLabel.Text = $"击杀: {kills} | 用时: {minutes}:{seconds:D2}";
        _glimmerLabel.Text = $"获得微光: {glimmerEarned}";
        _weaponLabel.Text = $"通关武器: {weaponName}";
    }
}
