using Godot;

namespace Miao.Weapon;

/// <summary>
/// 子弹实体
/// 由远程武器发射，碰到敌人造成伤害后消失
/// </summary>
public partial class Bullet : Area2D
{
    /// <summary>子弹速度</summary>
    public Vector2 Velocity;

    /// <summary>伤害值</summary>
    public int Damage;

    /// <summary>存活时间</summary>
    public float Lifetime = 3.0f;

    public override void _Ready()
    {
        // 设置碰撞检测
        CollisionLayer = 0;
        CollisionMask = 2; // 检测敌人

        var shape = new CircleShape2D();
        shape.Radius = 5f;

        var collision = new CollisionShape2D();
        collision.Shape = shape;
        AddChild(collision);

        // 连接碰撞信号
        BodyEntered += OnBodyEntered;
    }

    public override void _Process(double delta)
    {
        GlobalPosition += Velocity * (float)delta;
        Lifetime -= (float)delta;

        if (Lifetime <= 0)
        {
            QueueFree();
        }
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is Enemy.Enemy enemy)
        {
            enemy.TakeDamage(Damage);
            CallDeferred(nameof(Destroy));
        }
    }

    private void Destroy()
    {
        QueueFree();
    }
}
