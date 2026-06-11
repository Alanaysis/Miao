using Godot;
using Miao.UI;
using Miao.System;

namespace Miao.Lobby;

public partial class LobbyManager : Node2D
{
    private LobbyPlayer _player;
    private EquipmentScreen _equipScreen;
    private MetaShopUI _shopUI;
    private FireteamUI _fireteamUI;

    public override void _Ready()
    {
        _player = GetNode<LobbyPlayer>("LobbyPlayer");
        GD.Print("[LobbyManager] Ready - 开始创建设施");

        // Create shared UIs
        _equipScreen = new EquipmentScreen();
        AddChild(_equipScreen);

        _shopUI = new MetaShopUI();
        AddChild(_shopUI);

        _fireteamUI = new FireteamUI();
        AddChild(_fireteamUI);

        CreateFacilities();
        GD.Print("[LobbyManager] 所有设施创建完成");
    }

    private void CreateFacilities()
    {
        var facilities = GetNode<Node2D>("Facilities");

        // Equipment Machine
        var equipMachine = new EquipmentMachine();
        equipMachine.Position = new Vector2(300, 300);
        equipMachine.SetEquipmentScreen(_equipScreen);
        facilities.AddChild(equipMachine);

        // Map Sandbox
        var mapSandbox = new MapSandbox();
        mapSandbox.Position = new Vector2(640, 300);
        mapSandbox.SetEquipmentScreen(_equipScreen);
        facilities.AddChild(mapSandbox);

        // Meta Shop
        var metaShop = new MetaShopTerminal();
        metaShop.Position = new Vector2(980, 300);
        metaShop.SetShopUI(_shopUI);
        facilities.AddChild(metaShop);

        // Vault
        var vault = new Vault();
        vault.Position = new Vector2(300, 500);
        facilities.AddChild(vault);

        // Fireteam Communicator
        var fireteam = new FireteamCommunicator();
        fireteam.Position = new Vector2(640, 500);
        fireteam.SetFireteamUI(_fireteamUI);
        facilities.AddChild(fireteam);
    }
}
