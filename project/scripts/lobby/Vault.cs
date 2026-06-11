using Godot;

namespace Miao.Lobby;

public partial class Vault : LobbyFacility
{
    public override void _Ready()
    {
        FacilityName = "保险库";
        InteractText = "[E] 图鉴";
        base._Ready();

        var visual = new ColorRect();
        visual.Size = new Vector2(80, 80);
        visual.Position = new Vector2(-40, -40);
        visual.Color = new Color("#eab308");
        AddChild(visual);
    }

    public override void Interact()
    {
        GD.Print("打开图鉴/收藏系统");
        // TODO: create and show CollectionCodex UI
    }
}
