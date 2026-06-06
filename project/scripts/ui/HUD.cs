using Godot;

namespace Miao.UI;

/// <summary>
/// 游戏内HUD
/// 显示生命值、经验值、等级、游戏时间、击杀数
/// </summary>
public partial class HUD : CanvasLayer
{
    private Player.Player _player;
    private Label _levelLabel;
    private Label _timeLabel;
    private Label _killLabel;
    private ProgressBar _healthBar;
    private ProgressBar _expBar;

    public override void _Ready()
    {
        _player = GetTree().GetFirstNodeInGroup("player") as Player.Player;
        CreateUI();

        if (_player != null)
        {
            _player.HealthChanged += OnHealthChanged;
            _player.ExperienceChanged += OnExperienceChanged;
            _player.LevelUp += OnLevelUp;
        }
    }

    public override void _Process(double delta)
    {
        if (_player == null) return;

        // 更新游戏时间
        var time = Time.GetTicksMsec() / 1000.0f;
        var minutes = (int)(time / 60);
        var seconds = (int)(time % 60);
        _timeLabel.Text = $"时间: {minutes:D2}:{seconds:D2}";

        // 更新击杀数
        _killLabel.Text = $"击杀: {_player.KillCount}";
    }

    private void CreateUI()
    {
        // 根容器，铺满整个屏幕
        var root = new Control();
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(root);

        // 生命值标签
        var healthLabel = new Label();
        healthLabel.Text = "HP";
        healthLabel.Position = new Vector2(10, 8);
        root.AddChild(healthLabel);

        // 生命值条
        _healthBar = new ProgressBar();
        _healthBar.Position = new Vector2(35, 10);
        _healthBar.Size = new Vector2(180, 18);
        _healthBar.MinValue = 0;
        _healthBar.MaxValue = _player?.MaxHealth ?? 100;
        _healthBar.Value = _player?.CurrentHealth ?? 100;
        root.AddChild(_healthBar);

        // 经验值条（屏幕底部）
        _expBar = new ProgressBar();
        _expBar.Position = new Vector2(0, 700);
        _expBar.Size = new Vector2(1280, 14);
        _expBar.MinValue = 0;
        _expBar.MaxValue = _player?.ExperienceToLevel ?? 10;
        _expBar.Value = _player?.CurrentExperience ?? 0;
        root.AddChild(_expBar);

        // 等级标签（右上角）
        _levelLabel = new Label();
        _levelLabel.Text = $"等级: {_player?.Level ?? 1}";
        _levelLabel.Position = new Vector2(1130, 10);
        _levelLabel.Size = new Vector2(140, 25);
        _levelLabel.HorizontalAlignment = HorizontalAlignment.Right;
        root.AddChild(_levelLabel);

        // 时间标签
        _timeLabel = new Label();
        _timeLabel.Text = "时间: 00:00";
        _timeLabel.Position = new Vector2(1130, 35);
        _timeLabel.Size = new Vector2(140, 25);
        _timeLabel.HorizontalAlignment = HorizontalAlignment.Right;
        root.AddChild(_timeLabel);

        // 击杀标签
        _killLabel = new Label();
        _killLabel.Text = "击杀: 0";
        _killLabel.Position = new Vector2(1130, 60);
        _killLabel.Size = new Vector2(140, 25);
        _killLabel.HorizontalAlignment = HorizontalAlignment.Right;
        root.AddChild(_killLabel);
    }

    private void OnHealthChanged(int currentHealth, int maxHealth)
    {
        _healthBar.MaxValue = maxHealth;
        _healthBar.Value = currentHealth;
    }

    private void OnExperienceChanged(int currentExp, int expToLevel, int level)
    {
        _expBar.MaxValue = expToLevel;
        _expBar.Value = currentExp;
        _levelLabel.Text = $"等级: {level}";
    }

    private void OnLevelUp(int newLevel)
    {
        _levelLabel.Text = $"等级: {newLevel}";
    }
}
