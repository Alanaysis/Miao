using Godot;

namespace Miao.Weapon;

public partial class SwordSlash : Area2D
{
    private int _damage;
    private float _knockback;
    private float _lifetime = 0.15f;

    public void Init(int damage, float knockback)
    {
        _damage = damage;
        _knockback = knockback;
    }

    public override void _Ready()
    {
        CollisionLayer = 0;
        CollisionMask = 2;

        var shape = new CircleShape2D();
        shape.Radius = 60;
        var collision = new CollisionShape2D();
        collision.Shape = shape;
        AddChild(collision);

        // Damage enemies in arc
        foreach (var body in GetOverlappingBodies())
        {
            if (body is Enemy.Enemy enemy)
            {
                enemy.TakeDamage(_damage);
                enemy.ApplyKnockback(GlobalPosition.DirectionTo(enemy.GlobalPosition) * _knockback);
            }
        }
    }

    public override void _Process(double delta)
    {
        _lifetime -= (float)delta;
        if (_lifetime <= 0) QueueFree();
    }
}
