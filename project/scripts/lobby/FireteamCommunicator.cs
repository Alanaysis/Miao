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
        TexturePath = "res://assets/sprites/lobby/fireteam_comm.png";
        base._Ready();
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
    }
}
