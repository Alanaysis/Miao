using Godot;
using System.Collections.Generic;
using Miao.Enemy;

namespace Miao.System;

public partial class RoomGenerator : Node
{
    [Export] public PackedScene SmallBugScene;
    [Export] public PackedScene FastBugScene;
    [Export] public PackedScene TankBugScene;

    public int CurrentRoom { get; private set; }
    public int TotalRooms => 5; // 4普通 + 1Boss
    public bool IsBossRoom => CurrentRoom == 4;

    private readonly int[][] _waveCounts = new int[][]
    {
        new[] { 8, 10, 12 },           // 房间1: 3波
        new[] { 10, 12, 14, 16 },      // 房间2: 4波
        new[] { 8, 10, 6, 8, 10 },     // 房间3: 5波（含精英）
        new[] { 12, 14, 10, 12, 16 },  // 房间4: 5波
    };

    private int _currentWave;
    private int _enemiesRemaining;
    private Node2D _enemyContainer;
    private Node2D _player;

    [Signal]
    public delegate void RoomClearedEventHandler(int roomIndex);
    [Signal]
    public delegate void WaveStartedEventHandler(int waveIndex);
    [Signal]
    public delegate void AllRoomsClearedEventHandler();

    public override void _Ready()
    {
        _enemyContainer = new Node2D();
        _enemyContainer.Name = "Enemies";
        CallDeferred(nameof(AddEnemyContainer));
    }

    private void AddEnemyContainer()
    {
        GetTree().CurrentScene.AddChild(_enemyContainer);
    }

    public void StartRun(Node2D player)
    {
        _player = player;
        CurrentRoom = 0;
        StartRoom();
    }

    private void StartRoom()
    {
        _currentWave = 0;
        _enemiesRemaining = 0;
        GD.Print($"进入房间 {CurrentRoom + 1}/{TotalRooms}");
        SpawnWave();
    }

    private void SpawnWave()
    {
        if (IsBossRoom)
        {
            SpawnBoss();
            return;
        }

        var waves = _waveCounts[CurrentRoom];
        if (_currentWave >= waves.Length)
        {
            EmitSignal(SignalName.RoomCleared, CurrentRoom);
            return;
        }

        int count = waves[_currentWave];
        bool hasElite = (CurrentRoom == 2 && _currentWave >= 2);

        EmitSignal(SignalName.WaveStarted, _currentWave);

        for (int i = 0; i < count; i++)
        {
            var scene = ChooseEnemyType();
            var enemy = scene.Instantiate<Enemy>();
            enemy.GlobalPosition = GetSpawnPosition();
            enemy.RoomIndex = CurrentRoom;

            if (hasElite && i == 0)
            {
                enemy.IsElite = true;
                var mod = new EliteModifier();
                mod.Init(enemy, (EliteModType)GD.RandRange(0, 3));
                enemy.AddChild(mod);
            }

            enemy.EnemyDied += OnEnemyDied;
            _enemyContainer.AddChild(enemy);
            _enemiesRemaining++;
        }

        _currentWave++;
    }

    private void OnEnemyDied(int experience)
    {
        _enemiesRemaining--;
        if (_enemiesRemaining < 0) _enemiesRemaining = 0;
        if (_enemiesRemaining <= 0)
        {
            GetTree().CreateTimer(2.0).Timeout += SpawnWave;
        }
    }

    private void SpawnBoss()
    {
        EmitSignal(SignalName.WaveStarted, -1);

        var boss = GD.Load<PackedScene>("res://scenes/enemy/BugQueen.tscn").Instantiate<BugQueen>();
        boss.GlobalPosition = _player.GlobalPosition + new Vector2(0, -300);
        boss.SmallBugScene = SmallBugScene;
        boss.EnemyDied += OnBossDied;
        _enemyContainer.AddChild(boss);
    }

    private void OnBossDied(int experience)
    {
        GD.Print("虫后被击败！");
        EmitSignal(SignalName.RoomCleared, CurrentRoom);
        EmitSignal(SignalName.AllRoomsCleared);
    }

    private PackedScene ChooseEnemyType()
    {
        float roll = GD.Randf();
        if (CurrentRoom < 2)
        {
            return roll < 0.7f ? SmallBugScene : FastBugScene;
        }
        else
        {
            if (roll < 0.4f) return SmallBugScene;
            if (roll < 0.7f) return FastBugScene;
            return TankBugScene;
        }
    }

    private Vector2 GetSpawnPosition()
    {
        float distance = 400;
        float angle = GD.Randf() * Mathf.Tau;
        return _player.GlobalPosition + new Vector2(
            Mathf.Cos(angle) * distance,
            Mathf.Sin(angle) * distance
        );
    }

    public void AdvanceRoom()
    {
        CurrentRoom++;
        if (CurrentRoom >= TotalRooms)
        {
            EmitSignal(SignalName.AllRoomsCleared);
        }
        else
        {
            StartRoom();
        }
    }
}
