using Godot;

namespace Miao.Enemy;

/// <summary>
/// 快虫敌人
/// 低HP，快速，低碰撞伤害
/// </summary>
public partial class FastBug : Enemy
{
    public override void _Ready()
    {
        MoveSpeed = 140f;
        MaxHealth = 15;
        ContactDamage = 5;
        ExperienceValue = 4;
        base._Ready();
    }
}
