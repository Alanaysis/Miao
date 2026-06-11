using Godot;

namespace Miao.Lobby;

public partial class LobbyPlayer : CharacterBody2D
{
    [Export] public float MoveSpeed = 200.0f;

    private Sprite2D _sprite;
    private Label _interactPrompt;
    private IInteractable _nearbyInteractable;

    public override void _Ready()
    {
        _sprite = GetNode<Sprite2D>("Sprite2D");
        _interactPrompt = GetNode<Label>("InteractPrompt");
        _interactPrompt?.Hide();
    }

    public override void _PhysicsProcess(double delta)
    {
        var inputDir = Input.GetVector("move_left", "move_right", "move_up", "move_down");
        Velocity = inputDir * MoveSpeed;
        MoveAndSlide();

        if (inputDir.X != 0)
        {
            _sprite.FlipH = inputDir.X < 0;
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("interact") && _nearbyInteractable != null)
        {
            _nearbyInteractable.Interact();
        }
    }

    public void SetNearbyInteractable(IInteractable interactable)
    {
        _nearbyInteractable = interactable;
        if (_interactPrompt != null)
        {
            _interactPrompt.Visible = interactable != null;
        }
    }

    public void ClearNearbyInteractable()
    {
        _nearbyInteractable = null;
        _interactPrompt?.Hide();
    }
}
