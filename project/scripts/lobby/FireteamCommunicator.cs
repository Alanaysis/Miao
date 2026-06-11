using Godot;
using Miao.UI;

namespace Miao.Lobby;

public partial class FireteamCommunicator : LobbyFacility
{
    private FireteamUI _fireteamUI;

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

    public void SetFireteamUI(FireteamUI ui)
    {
        _fireteamUI = ui;
    }

    public override void Interact()
    {
        if (_fireteamUI != null)
        {
            _fireteamUI.Open();
        }
        else
        {
            GD.PushWarning("FireteamCommunicator: FireteamUI not set");
        }
    }
}
