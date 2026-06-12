using Godot;
using Miao.System;
using Miao.UI;

namespace Miao.Lobby;

public partial class MapSandbox : LobbyFacility
{
    private EquipmentScreen _equipScreen;

    public override void _Ready()
    {
        FacilityName = "地图沙盘";
        InteractText = "[E] 选择地图";
        TexturePath = "res://assets/sprites/game/lobby/facility_map_sandbox.png";
        base._Ready();
    }

    public void SetEquipmentScreen(EquipmentScreen screen)
    {
        _equipScreen = screen;
    }

    public override void Interact()
    {
        if (_equipScreen != null)
        {
            _equipScreen.OpenForMapSelection();
        }
        else
        {
            GameManager.Instance.StartGame();
        }
    }
}
