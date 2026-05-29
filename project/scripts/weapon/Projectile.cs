using Godot;

namespace Miao.Weapon;

/// <summary>
/// 远程弹射武器
/// 向最近的敌人发射子弹
/// </summary>
public partial class Projectile : Weapon
{
    /// <summary>子弹速度</summary>
    [Export] public float BulletSpeed = 400f;

    /// <summary>子弹场景</summary>
    [Export] public PackedScene BulletScene;

    /// <summary>子弹存活时间（秒）</summary>
    [Export] public float BulletLifetime = 3.0f;

    protected override void Attack()
    {
        var target = FindNearestEnemy();
        if (target == null) return;

        var bullet = CreateBullet();
        var direction = (target.GlobalPosition - GlobalPosition).Normalized();
        bullet.Velocity = direction * BulletSpeed;
        bullet.Damage = Damage;
    }

    private Enemy.Enemy FindNearestEnemy()
    {
        Enemy.Enemy nearest = null;
        float nearestDist = float.MaxValue;

        var enemies = GetTree().GetNodesInGroup("enemy");
        foreach (var node in enemies)
        {
            if (node is Enemy.Enemy enemy)
            {
                float dist = GlobalPosition.DistanceTo(enemy.GlobalPosition);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = enemy;
                }
            }
        }

        return nearest;
    }

    private Bullet CreateBullet()
    {
        var bullet = new Bullet();
        bullet.GlobalPosition = GlobalPosition;
        bullet.Lifetime = BulletLifetime;
        GetTree().CurrentScene.AddChild(bullet);
        return bullet;
    }
}
