using Godot;

namespace Miao.Lobby;

public partial class MapSandbox : LobbyFacility
{
    public override void _Ready()
    {
        FacilityName = "地图沙盘";
        InteractText = "[E] 选择地图";
        base._Ready();

        var visual = new ColorRect();
        visual.Size = new Vector2(80, 80);
        visual.Position = new Vector2(-40, -40);
        visual.Color = new Color("#22c55e");
        AddChild(visual);
    }

    public override void Interact()
    {
        GD.Print("打开地图选择");
        // TODO: open map selection UI
    }
}
