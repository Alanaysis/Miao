using Godot;
using Miao.UI;

namespace Miao.Lobby;

public partial class MetaShopTerminal : LobbyFacility
{
    private MetaShopUI _shopUI;

    public override void _Ready()
    {
        FacilityName = "Meta商店";
        InteractText = "[E] Meta商店";
        base._Ready();

        var visual = new ColorRect();
        visual.Size = new Vector2(80, 80);
        visual.Position = new Vector2(-40, -40);
        visual.Color = new Color("#a855f7");
        AddChild(visual);
    }

    public void SetShopUI(MetaShopUI ui)
    {
        _shopUI = ui;
    }

    public override void Interact()
    {
        if (_shopUI != null)
        {
            _shopUI.Open();
        }
    }
}
