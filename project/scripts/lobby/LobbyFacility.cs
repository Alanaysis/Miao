using Godot;

namespace Miao.Lobby;

public abstract partial class LobbyFacility : Area2D, IInteractable
{
    [Export] public string FacilityName = "设施";
    [Export] public string InteractText = "[E] 互动";
    [Export] public string TexturePath = "";

    private Label _label;
    private Sprite2D _sprite;

    public override void _Ready()
    {
        CollisionLayer = 4;
        CollisionMask = 1;

        // 添加碰撞体
        var shape = new CircleShape2D();
        shape.Radius = 60;
        var collision = new CollisionShape2D();
        collision.Shape = shape;
        AddChild(collision);

        // 加载贴图
        if (!string.IsNullOrEmpty(TexturePath) && FileAccess.FileExists(TexturePath))
        {
            _sprite = new Sprite2D();
            _sprite.Texture = GD.Load<Texture2D>(TexturePath);
            AddChild(_sprite);
        }
        else
        {
            // 回退：彩色方块
            var visual = new ColorRect();
            visual.Size = new Vector2(64, 64);
            visual.Position = new Vector2(-32, -32);
            visual.Color = new Color(0.3f, 0.3f, 0.3f);
            AddChild(visual);
        }

        // 交互提示标签
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
