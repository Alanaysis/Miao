using Godot;

namespace Miao.Weapon;

/// <summary>
/// 旋转刀刃武器
/// 围绕玩家旋转，碰到敌人造成伤害
/// </summary>
public partial class SpinBlade : Weapon
{
    /// <summary>旋转半径</summary>
    [Export] public float SpinRadius = 60f;

    /// <summary>旋转速度（弧度/秒）</summary>
    [Export] public float SpinSpeed = 3.0f;

    /// <summary>刀刃碰撞体</summary>
    private Area2D _bladeArea;
    private float _angle;

    public override void _Ready()
    {
        base._Ready();
        CreateBladeArea();
    }

    public override void _Process(double delta)
    {
        _angle += SpinSpeed * (float)delta;
        Position = new Vector2(
            Mathf.Cos(_angle) * SpinRadius,
            Mathf.Sin(_angle) * SpinRadius
        );

        // 检测碰撞
        CheckCollisions();
    }

    private void CreateBladeArea()
    {
        _bladeArea = new Area2D();
        _bladeArea.CollisionLayer = 0;
        _bladeArea.CollisionMask = 2; // 检测敌人层

        var shape = new CircleShape2D();
        shape.Radius = 12f;

        var collision = new CollisionShape2D();
        collision.Shape = shape;
        _bladeArea.AddChild(collision);

        AddChild(_bladeArea);
    }

    private void CheckCollisions()
    {
        var bodies = _bladeArea.GetOverlappingBodies();
        foreach (var body in bodies)
        {
            if (body is Enemy.Enemy enemy)
            {
                enemy.TakeDamage(Damage);
            }
        }
    }
}
