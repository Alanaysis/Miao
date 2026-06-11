using Godot;

namespace Miao.Lobby;

public partial class EquipmentMachine : LobbyFacility
{
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

    public override void Interact()
    {
        GD.Print("打开装备配置界面");
        // TODO: open EquipmentScreen
    }
}
