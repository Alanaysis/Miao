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
        base._Ready();

        var visual = new ColorRect();
        visual.Size = new Vector2(80, 80);
        visual.Position = new Vector2(-40, -40);
        visual.Color = new Color("#3b82f6");
        AddChild(visual);
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
        else
        {
            GD.PushWarning("EquipmentMachine: EquipmentScreen not set");
        }
    }
}
