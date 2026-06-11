using Godot;

namespace Miao.Lobby;

public partial class LobbyManager : Node2D
{
    private LobbyPlayer _player;

    public override void _Ready()
    {
        _player = GetNode<LobbyPlayer>("LobbyPlayer");

        CreateFacilities();
    }

    private void CreateFacilities()
    {
        var facilities = GetNode<Node2D>("Facilities");

        var equipMachine = new EquipmentMachine();
        equipMachine.Position = new Vector2(300, 300);
        facilities.AddChild(equipMachine);

        var mapSandbox = new MapSandbox();
        mapSandbox.Position = new Vector2(640, 300);
        facilities.AddChild(mapSandbox);

        var metaShop = new MetaShopTerminal();
        metaShop.Position = new Vector2(980, 300);
        facilities.AddChild(metaShop);

        var vault = new Vault();
        vault.Position = new Vector2(300, 500);
        facilities.AddChild(vault);

        var fireteam = new FireteamCommunicator();
        fireteam.Position = new Vector2(640, 500);
        facilities.AddChild(fireteam);
    }
}
