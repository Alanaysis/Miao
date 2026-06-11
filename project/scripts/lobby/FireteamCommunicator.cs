using Godot;

namespace Miao.Lobby;

public partial class FireteamCommunicator : LobbyFacility
{
    public override void _Ready()
    {
        FacilityName = "火力队通讯器";
        InteractText = "[E] 火力队";
        base._Ready();

        var visual = new ColorRect();
        visual.Size = new Vector2(80, 80);
        visual.Position = new Vector2(-40, -40);
        visual.Color = new Color("#ef4444");
        AddChild(visual);
    }

    public override void Interact()
    {
        GD.Print("打开火力队通讯");
        // TODO: open Fireteam UI
    }
}
