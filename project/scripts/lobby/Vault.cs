using Godot;
using Miao.UI;

namespace Miao.Lobby;

public partial class Vault : LobbyFacility
{
    private CodexUI _codexUI;

    public override void _Ready()
    {
        FacilityName = "保险库";
        InteractText = "[E] 图鉴";
        TexturePath = "res://assets/sprites/game/lobby/facility_vault.png";
        base._Ready();
    }

    public void SetCodexUI(CodexUI ui)
    {
        _codexUI = ui;
    }

    public override void Interact()
    {
        if (_codexUI != null)
        {
            _codexUI.Open();
        }
    }
}
