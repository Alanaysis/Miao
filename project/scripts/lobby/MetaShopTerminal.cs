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
        TexturePath = "res://assets/sprites/game/lobby/facility_meta_shop.png";
        base._Ready();
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
