using Godot;

namespace Miao.Weapon;

/// <summary>
/// 武器基类
/// 所有武器继承此类，实现自动攻击逻辑
/// </summary>
public partial class Weapon : Node2D
{
    /// <summary>武器伤害</summary>
    [Export] public int Damage = 10;

    /// <summary>攻击间隔（秒）</summary>
    [Export] public float AttackCooldown = 1.0f;

    /// <summary>攻击速度倍率（由升级系统修改）</summary>
    public float AttackSpeedMultiplier = 1.0f;

    protected float _cooldownTimer;
    protected Node2D _owner;

    public override void _Ready()
    {
        _owner = GetParent() as Node2D;
    }

    public override void _Process(double delta)
    {
        _cooldownTimer -= (float)delta;

        if (_cooldownTimer <= 0)
        {
            Attack();
            _cooldownTimer = AttackCooldown / AttackSpeedMultiplier;
        }
    }

    /// <summary>
    /// 执行攻击（子类重写）
    /// </summary>
    protected virtual void Attack()
    {
        // 子类实现具体攻击逻辑
    }
}
