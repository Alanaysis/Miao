using Godot;

namespace Miao.Network;

public partial class PlayerSpawner : MultiplayerSpawner
{
    [Export] public PackedScene PlayerScene;

    public override void _Ready()
    {
        SpawnFunction = new Callable(this, nameof(SpawnPlayer));
    }

    private Node SpawnPlayer(int id)
    {
        var player = PlayerScene.Instantiate();
        player.Name = id.ToString();
        player.SetMultiplayerAuthority(id);
        // Set position based on player count
        var players = GetTree().GetNodesInGroup("player");
        player.GetNode<CharacterBody2D>(".").GlobalPosition = new Vector2(400 + players.Count * 100, 360);
        return player;
    }
}
