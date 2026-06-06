using Godot;
using System.Collections.Generic;
using Miao.Enemy;

namespace Miao.Weapon;

public partial class Bullet : Area2D
{
    public Vector2 Velocity { get; set; }
    public int Damage { get; set; }
    public float Knockback { get; set; }
    public bool CanPenetrate { get; set; }
    public float Lifetime { get; set; } = 3.0f;

    private float _lifetime;
    private HashSet<Node2D> _hitTargets = new();

    public override void _Ready()
    {
        _lifetime = Lifetime;
        CollisionLayer = 0;
        CollisionMask = 2; // 敌人层

        var shape = new CircleShape2D();
        shape.Radius = 5;
        var collision = new CollisionShape2D();
        collision.Shape = shape;
        AddChild(collision);

        BodyEntered += OnBodyEntered;
    }

    public override void _Process(double delta)
    {
        Position += Velocity * (float)delta;
        _lifetime -= (float)delta;
        if (_lifetime <= 0) QueueFree();
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is Enemy enemy && !_hitTargets.Contains(body))
        {
            _hitTargets.Add(body);
            enemy.TakeDamage(Damage);
            enemy.ApplyKnockback(Velocity.Normalized() * Knockback);

            if (!CanPenetrate)
            {
                QueueFree();
            }
        }
    }
}
