using Godot;

namespace Miao.Enemy;

/// <summary>
/// 小虫敌人
/// 低HP，慢速，基础碰撞伤害
/// </summary>
public partial class SmallBug : Enemy
{
    public override void _Ready()
    {
        MoveSpeed = 60f;
        MaxHealth = 20;
        ContactDamage = 8;
        ExperienceValue = 3;
        base._Ready();
    }
}
