using Godot;
using Miao.UI;

namespace Miao.Lobby;

public partial class EquipmentMachine : LobbyFacility
{
    private EquipmentScreen _equipScreen;

    public override void _Ready()
    {
        FacilityName = "装备配置机器";
        InteractText = "[E] 装备配置";
        TexturePath = "res://assets/sprites/game/lobby/facility_equipment.png";
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
            _equipScreen.Open();
        }
    }
}
