using Godot;

namespace Miao.Lobby;

public partial class FireteamCommunicator : LobbyFacility
{
    public override void _Ready()
    {
        FacilityName = "火力战队通讯仪";
        InteractText = "[E] 多人联机";
        base._Ready();

        var visual = new ColorRect();
        visual.Size = new Vector2(80, 80);
        visual.Position = new Vector2(-40, -40);
        visual.Color = new Color("#ef4444");
        AddChild(visual);
    }

    public override void Interact()
    {
        GD.Print("打开多人联机界面");
        // TODO: create and show FireteamUI
    }
}
