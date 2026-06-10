using Godot;

namespace Miao.Player;

public partial class SolarFireTrail : Area2D
{
    private float _lifetime = 3.0f;
    private float _damagePerSecond = 5.0f;
    private float _damageTimer = 0;

    public override void _Ready()
    {
        CollisionLayer = 0;
        CollisionMask = 2; // enemies

        var shape = new CircleShape2D();
        shape.Radius = 20;
        var collision = new CollisionShape2D();
        collision.Shape = shape;
        AddChild(collision);

        // Visual: orange semi-transparent circle
        var colorRect = new ColorRect();
        colorRect.Size = new Vector2(40, 40);
        colorRect.Position = new Vector2(-20, -20);
        colorRect.Color = new Color(1, 0.5f, 0, 0.3f);
        AddChild(colorRect);
    }

    public override void _Process(double delta)
    {
        _lifetime -= (float)delta;
        if (_lifetime <= 0)
        {
            QueueFree();
            return;
        }

        _damageTimer -= (float)delta;
        if (_damageTimer <= 0)
        {
            _damageTimer = 0.5f; // damage every 0.5s
            foreach (var body in GetOverlappingBodies())
            {
                if (body is Enemy.Enemy enemy)
                {
                    enemy.TakeDamage(Mathf.RoundToInt(_damagePerSecond * 0.5f));
                }
            }
        }
    }
}
