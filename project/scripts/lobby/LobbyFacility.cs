using Godot;

namespace Miao.Lobby;

public abstract partial class LobbyFacility : Area2D, IInteractable
{
    [Export] public string FacilityName = "设施";
    [Export] public string InteractText = "[E] 互动";

    private Label _label;

    public override void _Ready()
    {
        CollisionLayer = 4;
        CollisionMask = 1;

        // 添加碰撞体（检测玩家进入范围）
        var shape = new CircleShape2D();
        shape.Radius = 60;
        var collision = new CollisionShape2D();
        collision.Shape = shape;
        AddChild(collision);

        // 交互提示标签（显示在设施上方）
        _label = new Label();
        _label.Text = InteractText;
        _label.Position = new Vector2(-50, -60);
        _label.AddThemeFontSizeOverride("font_size", 14);
        _label.Hide();
        AddChild(_label);

        BodyEntered += OnBodyEntered;
        BodyExited += OnBodyExited;
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is LobbyPlayer player)
        {
            player.SetNearbyInteractable(this);
            _label.Show();
        }
    }

    private void OnBodyExited(Node2D body)
    {
        if (body is LobbyPlayer player)
        {
            player.ClearNearbyInteractable();
            _label.Hide();
        }
    }

    public abstract void Interact();
}
