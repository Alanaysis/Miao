using Godot;

namespace Miao.System;

/// <summary>
/// 敌人生成管理器
/// 负责定时生成敌人，随时间增加难度
/// </summary>
public partial class SpawnManager : Node
{
    /// <summary>小虫场景</summary>
    [Export] public PackedScene SmallBugScene;

    /// <summary>快虫场景</summary>
    [Export] public PackedScene FastBugScene;

    /// <summary>坦克场景</summary>
    [Export] public PackedScene TankBugScene;

    /// <summary>初始生成间隔（秒）</summary>
    [Export] public float InitialSpawnInterval = 2.0f;

    /// <summary>最小生成间隔（秒）</summary>
    [Export] public float MinSpawnInterval = 0.3f;

    /// <summary>生成间隔递减速率（每秒减少的间隔）</summary>
    [Export] public float SpawnIntervalDecrease = 0.02f;

    /// <summary>生成距离（离玩家多远生成）</summary>
    [Export] public float SpawnDistance = 500f;

    private float _spawnTimer;
    private float _currentSpawnInterval;
    private float _gameTime;
    private Node2D _player;
    private Node _enemyContainer;

    public override void _Ready()
    {
        _currentSpawnInterval = InitialSpawnInterval;
        _spawnTimer = _currentSpawnInterval;
        _player = GetTree().GetFirstNodeInGroup("player") as Node2D;

        // 创建敌人容器（延迟添加避免 _Ready 冲突）
        _enemyContainer = new Node2D();
        _enemyContainer.Name = "Enemies";
        CallDeferred(nameof(AddEnemyContainer));
    }

    public override void _Process(double delta)
    {
        if (_player == null) return;

        _gameTime += (float)delta;
        _spawnTimer -= (float)delta;

        // 递减生成间隔
        _currentSpawnInterval = Mathf.Max(
            MinSpawnInterval,
            InitialSpawnInterval - _gameTime * SpawnIntervalDecrease
        );

        if (_spawnTimer <= 0)
        {
            SpawnEnemy();
            _spawnTimer = _currentSpawnInterval;
        }
    }

    private void SpawnEnemy()
    {
        // 根据游戏时间决定敌人类型权重
        var scene = ChooseEnemyScene();
        if (scene == null) return;

        var enemy = scene.Instantiate<Node2D>();
        enemy.GlobalPosition = GetSpawnPosition();
        _enemyContainer.AddChild(enemy);
    }

    private PackedScene ChooseEnemyScene()
    {
        // 随时间解锁更强的敌人
        float rand = GD.Randf();

        if (_gameTime < 30)
        {
            // 前30秒只有小虫
            return SmallBugScene;
        }
        else if (_gameTime < 90)
        {
            // 30-90秒：小虫为主，偶尔快虫
            return rand < 0.7f ? SmallBugScene : FastBugScene;
        }
        else
        {
            // 90秒后：三种敌人都出
            if (rand < 0.5f) return SmallBugScene;
            if (rand < 0.8f) return FastBugScene;
            return TankBugScene;
        }
    }

    private Vector2 GetSpawnPosition()
    {
        // 在玩家周围随机位置生成（视野外）
        float angle = GD.Randf() * Mathf.Tau;
        return _player.GlobalPosition + new Vector2(
            Mathf.Cos(angle) * SpawnDistance,
            Mathf.Sin(angle) * SpawnDistance
        );
    }

    private void AddEnemyContainer()
    {
        GetTree().CurrentScene.AddChild(_enemyContainer);
    }
}
