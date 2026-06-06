using Godot;

namespace Miao.System;

/// <summary>
/// 游戏管理器
/// 管理游戏状态、场景切换、游戏结束处理
/// </summary>
public partial class GameManager : Node
{
    public enum GameState
    {
        MainMenu,
        Playing,
        Paused,
        GameOver
    }

    public static GameManager Instance { get; private set; }

    public GameState CurrentState { get; private set; } = GameState.MainMenu;

    private Player.Player _player;
    private float _gameTime;
    private int _killCount;

    public override void _Ready()
    {
        Instance = this;
        InputConfig.Load();
    }

    public override void _Process(double delta)
    {
        if (CurrentState == GameState.Playing)
        {
            _gameTime += (float)delta;
        }
    }

    /// <summary>
    /// 开始新游戏
    /// </summary>
    public void StartGame()
    {
        CurrentState = GameState.Playing;
        _gameTime = 0;
        _killCount = 0;
        GetTree().ChangeSceneToFile("res://scenes/main.tscn");
    }

    private float _gameTimeAtDeath;
    private int _killCountAtDeath;
    private int _goldReward;

    /// <summary>
    /// 游戏结束
    /// </summary>
    public void GameOver()
    {
        CurrentState = GameState.GameOver;
        _gameTimeAtDeath = _gameTime;
        _killCountAtDeath = _killCount;

        // 计算金币奖励
        _goldReward = 0;
        if (MetaProgression.Instance != null)
        {
            _goldReward = MetaProgression.Instance.CalculateGoldReward(_gameTime, _killCount);
            MetaProgression.Instance.AddGold(_goldReward);
        }

        GetTree().Paused = true;
        CallDeferred(nameof(ShowGameOverUI));
    }

    private void ShowGameOverUI()
    {
        var gameOverUI = new UI.GameOverUI();
        gameOverUI.SetStats(_gameTimeAtDeath, _killCountAtDeath, _goldReward);
        GetTree().CurrentScene.AddChild(gameOverUI);
    }

    /// <summary>
    /// 返回主菜单
    /// </summary>
    public void ReturnToMainMenu()
    {
        CurrentState = GameState.MainMenu;
        GetTree().Paused = false;
        GetTree().ChangeSceneToFile("res://scenes/ui/MainMenu.tscn");
    }

    /// <summary>
    /// 注册玩家（由Player._Ready调用）
    /// </summary>
    public void RegisterPlayer(Player.Player player)
    {
        _player = player;
        _player.PlayerDied += OnPlayerDied;
    }

    private void OnPlayerDied()
    {
        GameOver();
    }

    public float GetGameTime() => _gameTime;
    public int GetKillCount() => _killCount;
}
