using Godot;
using Miao.Player;

namespace Miao.System;

public partial class GameManager : Node
{
    public static GameManager Instance { get; private set; }

    public enum GameState { MainMenu, Playing, RoomTransition, Boss, Settlement, GameOver }
    public GameState CurrentState { get; private set; }

    private Player.Player _player;
    private RoomGenerator _roomGenerator;
    private float _gameTime;
    private int _killCount;
    private int _glimmer;

    [Signal]
    public delegate void GlimmerChangedEventHandler(int amount);

    public override void _Ready()
    {
        if (Instance != null && Instance != this)
        {
            QueueFree();
            return;
        }
        Instance = this;
        InputConfig.Load();
    }

    public override void _ExitTree()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public override void _Process(double delta)
    {
        if (CurrentState == GameState.Playing)
        {
            _gameTime += (float)delta;
        }
    }

    public void StartGame()
    {
        CurrentState = GameState.Playing;
        _gameTime = 0;
        _killCount = 0;
        GetTree().ChangeSceneToFile("res://scenes/main.tscn");
    }

    public void RegisterPlayer(Player.Player player)
    {
        _player = player;
        player.PlayerDied += OnPlayerDied;
    }

    public void RegisterRoomGenerator(RoomGenerator rg)
    {
        _roomGenerator = rg;
        rg.RoomCleared += OnRoomCleared;
        rg.AllRoomsCleared += OnAllRoomsCleared;
        rg.StartRun(_player);
    }

    private void OnRoomCleared(int roomIndex)
    {
        CurrentState = GameState.RoomTransition;
        GD.Print($"房间 {roomIndex + 1} 清空！按 E 进入下一房间");
    }

    public void NextRoom()
    {
        CurrentState = GameState.Playing;
        _roomGenerator.AdvanceRoom();
    }

    private void OnAllRoomsCleared()
    {
        CurrentState = GameState.Settlement;
        GD.Print("所有房间清空！进入结算");
    }

    private void OnPlayerDied()
    {
        CurrentState = GameState.GameOver;
        GetTree().Paused = true;
    }

    public void AddGlimmer(int amount)
    {
        _glimmer += amount;
        EmitSignal(SignalName.GlimmerChanged, _glimmer);
    }

    public void AddKill()
    {
        _killCount++;
    }

    public int GetGlimmer() => _glimmer;
    public float GetGameTime() => _gameTime;
    public int GetKillCount() => _killCount;

    public void ReturnToMainMenu()
    {
        GetTree().Paused = false;
        CurrentState = GameState.MainMenu;
        GetTree().ChangeSceneToFile("res://scenes/ui/MainMenu.tscn");
    }
}
