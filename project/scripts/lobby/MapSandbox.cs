using Godot;
using Miao.System;
using Miao.UI;

namespace Miao.Lobby;

public partial class MapSandbox : LobbyFacility
{
    private EquipmentScreen _equipScreen;

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

    public void SetEquipmentScreen(EquipmentScreen screen)
    {
        _equipScreen = screen;
    }

    public override void Interact()
    {
        // Open equipment screen with map selection
        if (_equipScreen != null)
        {
            _equipScreen.OpenForMapSelection();
        }
        else
        {
            // Fallback: start game directly
            GameManager.Instance.StartGame();
        }
    }
}
