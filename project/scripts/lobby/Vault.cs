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
        base._Ready();

        var visual = new ColorRect();
        visual.Size = new Vector2(80, 80);
        visual.Position = new Vector2(-40, -40);
        visual.Color = new Color("#eab308");
        AddChild(visual);
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
