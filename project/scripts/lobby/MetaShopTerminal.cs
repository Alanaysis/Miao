using Godot;

namespace Miao.Lobby;

public partial class MetaShopTerminal : LobbyFacility
{
    public override void _Ready()
    {
        FacilityName = "元商店终端";
        InteractText = "[E] 元商店";
        base._Ready();

        var visual = new ColorRect();
        visual.Size = new Vector2(80, 80);
        visual.Position = new Vector2(-40, -40);
        visual.Color = new Color("#a855f7");
        AddChild(visual);
    }

    public override void Interact()
    {
        GD.Print("打开元商店");
        // TODO: open MetaShop UI
    }
}
