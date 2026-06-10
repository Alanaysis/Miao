using Godot;

namespace Miao.Weapon;

public partial class RocketExplosion : Area2D
{
    private int _damage;
    private float _radius;
    private float _lifetime = 0.3f;

    public void Init(int damage, float radius)
    {
        _damage = damage;
        _radius = radius;
    }

    public override void _Ready()
    {
        CollisionLayer = 0;
        CollisionMask = 2;

        var shape = new CircleShape2D();
        shape.Radius = _radius;
        var collision = new CollisionShape2D();
        collision.Shape = shape;
        AddChild(collision);

        // Visual explosion
        var circle = new ColorRect();
        circle.Size = new Vector2(_radius * 2, _radius * 2);
        circle.Position = new Vector2(-_radius, -_radius);
        circle.Color = new Color(1, 0.5f, 0, 0.6f);
        AddChild(circle);

        // Damage all enemies in range immediately
        foreach (var body in GetOverlappingBodies())
        {
            if (body is Enemy.Enemy enemy)
            {
                enemy.TakeDamage(_damage);
            }
        }
    }

    public override void _Process(double delta)
    {
        _lifetime -= (float)delta;
        if (_lifetime <= 0) QueueFree();
    }
}
