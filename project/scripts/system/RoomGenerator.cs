using Godot;
using System.Collections.Generic;
using Miao.Data;
using Miao.Enemy;
using Miao.Pickup;
using Miao.Weapon;

namespace Miao.System;

public partial class RoomGenerator : Node
{
    [Export] public PackedScene SmallBugScene;
    [Export] public PackedScene FastBugScene;
    [Export] public PackedScene TankBugScene;

    public int CurrentRoom { get; private set; }
    public int TotalRooms => 5; // 4普通 + 1Boss
    public bool IsBossRoom => CurrentRoom == 4;
    public string CurrentMapId { get; set; } = "nest"; // Default map

    // 当前地图配置
    private MapConfig _currentMapConfig;

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
    private Vector2 _bossPosition;

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

        // 加载地图配置
        LoadMapConfig();

        // 应用地图主题
        ApplyMapTheme();

        StartRoom();
    }

    private void LoadMapConfig()
    {
        var mapsData = DataLoader.Load<MapsConfig>("maps.json");
        if (mapsData?.Maps != null)
        {
            foreach (var map in mapsData.Maps)
            {
                if (map.Id == CurrentMapId)
                {
                    _currentMapConfig = map;
                    GD.Print($"加载地图配置: {map.Name} (难度 {map.Difficulty}, Boss: {map.Boss})");
                    return;
                }
            }
        }

        // Fallback: use default nest config
        GD.PushWarning($"未找到地图 {CurrentMapId}，使用默认配置");
    }

    private void ApplyMapTheme()
    {
        string theme = _currentMapConfig?.Theme ?? "hive";
        var scene = GetTree().CurrentScene;

        scene.GetNodeOrNull<MapBackground>("MapBackground")?.SetMapTheme(theme);
        scene.GetNodeOrNull<MapBoundary>("MapBoundary")?.SetMapTheme(theme);
    }

    private void StartRoom()
    {
        _currentWave = 0;
        _enemiesRemaining = 0;
        GD.Print($"进入房间 {CurrentRoom + 1}/{TotalRooms}");

        // 切换房间主题（地图主题下房间仍有细微变化）
        var scene = GetTree().CurrentScene;
        scene.GetNodeOrNull<MapBackground>("MapBackground")?.SetRoomTheme(CurrentRoom);
        scene.GetNodeOrNull<MapBoundary>("MapBoundary")?.SetRoomTheme(CurrentRoom);

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
            var enemy = scene.Instantiate<Enemy.Enemy>();
            enemy.GlobalPosition = GetSpawnPosition();
            enemy.RoomIndex = CurrentRoom;

            enemy.EnemyDied += OnEnemyDied;

            if (hasElite && i == 0)
            {
                enemy.IsElite = true;
                var mod = new EliteModifier();
                mod.Init(enemy, (EliteModType)GD.RandRange(0, 3), OnSplitEnemySpawned);
                enemy.AddChild(mod);
            }
            _enemyContainer.AddChild(enemy);
            _enemiesRemaining++;
        }

        _currentWave++;
    }

    private void OnSplitEnemySpawned(Enemy.Enemy enemy)
    {
        enemy.EnemyDied += OnEnemyDied;
        _enemiesRemaining++;
    }

    private void OnEnemyDied(int experience)
    {
        _enemiesRemaining--;
        if (_enemiesRemaining < 0) _enemiesRemaining = 0;
        if (_enemiesRemaining <= 0)
        {
            // 双重检查：确保场景树中真的没有敌人了
            if (_enemyContainer.GetChildCount() <= 0)
            {
                GetTree().CreateTimer(2.0).Timeout += SpawnWave;
            }
            else
            {
                // 还有敌人（可能是计数偏差），等一帧再检查
                GetTree().CreateTimer(0.5).Timeout += () =>
                {
                    if (_enemyContainer.GetChildCount() <= 0)
                        SpawnWave();
                };
            }
        }
    }

    private void SpawnBoss()
    {
        EmitSignal(SignalName.WaveStarted, -1);

        // 根据地图 boss 配置加载对应 boss 场景
        string bossId = _currentMapConfig?.Boss ?? "bug_queen";
        var bossScene = LoadBossScene(bossId);

        if (bossScene == null)
        {
            GD.PushError($"未找到 Boss 场景: {bossId}，使用默认 BugQueen");
            bossScene = GD.Load<PackedScene>("res://scenes/enemy/BugQueen.tscn");
        }

        var boss = bossScene.Instantiate<BugQueen>();
        boss.GlobalPosition = _player.GlobalPosition + new Vector2(0, -300);
        _bossPosition = boss.GlobalPosition;
        boss.SmallBugScene = SmallBugScene;
        boss.EnemyDied += OnBossDied;
        _enemyContainer.AddChild(boss);
    }

    private PackedScene LoadBossScene(string bossId)
    {
        // 尝试加载对应 boss 场景，不存在则返回 null 使用默认
        string path = $"res://scenes/enemy/{BossIdToSceneName(bossId)}.tscn";
        if (ResourceLoader.Exists(path))
        {
            return GD.Load<PackedScene>(path);
        }
        return null;
    }

    private static string BossIdToSceneName(string bossId)
    {
        return bossId switch
        {
            "bug_queen" => "BugQueen",
            "ruin_guardian" => "RuinGuardian",
            "crystal_worm" => "CrystalWorm",
            "void_lord" => "VoidLord",
            "flame_emperor" => "FlameEmperor",
            _ => "BugQueen"
        };
    }

    private void OnBossDied(int experience)
    {
        string bossName = _currentMapConfig?.Boss ?? "虫后";
        GD.Print($"{bossName} 被击败！");

        // Boss 掉落 1-2 个史诗/传说记忆水晶
        int engramCount = GD.RandRange(1, 2);
        for (int i = 0; i < engramCount; i++)
        {
            var rarity = GD.Randf() < 0.3f ? Rarity.Legendary : Rarity.Epic;
            var weaponData = LootTable.GenerateWeapon(CurrentRoom, true);
            var engram = new MemoryEngram();
            engram.GlobalPosition = _bossPosition + new Vector2(GD.RandRange(-30, 30), GD.RandRange(-30, 30));
            engram.Init(weaponData, rarity);
            GetTree().CurrentScene.AddChild(engram);
        }

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
            CleanupRoom();
            StartRoom();
        }
    }

    private void CleanupRoom()
    {
        // 清理所有敌人
        foreach (var child in _enemyContainer.GetChildren())
        {
            child.QueueFree();
        }
        _enemiesRemaining = 0;

        // 清理所有武器掉落
        if (GameManager.Instance != null)
        {
            GameManager.Instance.CleanupWeaponDrops();
        }
    }
}
