using Godot;

namespace Miao.Enemy;

/// <summary>
/// 坦克敌人
/// 高HP，慢速，高碰撞伤害
/// </summary>
public partial class TankBug : Enemy
{
    public override void _Ready()
    {
        MoveSpeed = 40f;
        MaxHealth = 80;
        ContactDamage = 20;
        ExperienceValue = 10;
        base._Ready();
    }
}
