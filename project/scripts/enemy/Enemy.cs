using Godot;

namespace Miao.Enemy;

/// <summary>
/// 敌人基类
/// 负责向玩家移动、受伤、死亡、掉落经验
/// </summary>
public partial class Enemy : CharacterBody2D
{
    /// <summary>移动速度</summary>
    [Export] public float MoveSpeed = 80f;

    /// <summary>最大生命值</summary>
    [Export] public int MaxHealth = 30;

    /// <summary>碰撞伤害</summary>
    [Export] public int ContactDamage = 10;

    /// <summary>经验掉落值</summary>
    [Export] public int ExperienceValue = 5;

    /// <summary>受伤冷却时间（秒）</summary>
    [Export] public float DamageCooldown = 1.0f;

    /// <summary>击退力度</summary>
    [Export] public float KnockbackForce = 300f;

    /// <summary>经验球场景</summary>
    [Export] public PackedScene ExperienceOrbScene;

    public int CurrentHealth { get; private set; }

    private Node2D _player;
    private float _damageCooldownTimer;
    private Vector2 _knockbackVelocity;

    [Signal]
    public delegate void EnemyDiedEventHandler(int experienceValue);

    public override void _Ready()
    {
        CurrentHealth = MaxHealth;
        _player = GetTree().GetFirstNodeInGroup("player") as Node2D;
        AddToGroup("enemy");
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_player == null) return;

        // 受伤冷却计时
        if (_damageCooldownTimer > 0)
        {
            _damageCooldownTimer -= (float)delta;
        }

        // 击退衰减
        _knockbackVelocity = _knockbackVelocity.Lerp(Vector2.Zero, 10f * (float)delta);

        // 向玩家移动 + 击退
        var direction = (_player.GlobalPosition - GlobalPosition).Normalized();
        Velocity = direction * MoveSpeed + _knockbackVelocity;
        MoveAndSlide();

        // 检测与玩家碰撞
        for (int i = 0; i < GetSlideCollisionCount(); i++)
        {
            var collision = GetSlideCollision(i);
            if (collision.GetCollider() is Player.Player player && _damageCooldownTimer <= 0)
            {
                player.TakeDamage(ContactDamage);
                _damageCooldownTimer = DamageCooldown;

                // 击退：向远离玩家方向弹开
                _knockbackVelocity = -direction * KnockbackForce;
            }
        }
    }

    /// <summary>
    /// 对敌人施加击退力
    /// </summary>
    /// <param name="force">击退力向量</param>
    public void ApplyKnockback(Vector2 force)
    {
        _knockbackVelocity += force;
    }

    /// <summary>
    /// 对敌人造成伤害
    /// </summary>
    /// <param name="damage">伤害值</param>
    public void TakeDamage(int damage)
    {
        CurrentHealth -= damage;

        if (CurrentHealth <= 0)
        {
            CallDeferred(nameof(Die));
        }
    }

    private void Die()
    {
        EmitSignal(SignalName.EnemyDied, ExperienceValue);

        // 掉落经验球
        if (ExperienceOrbScene != null)
        {
            var orb = ExperienceOrbScene.Instantiate<Node2D>();
            orb.GlobalPosition = GlobalPosition;
            GetTree().CurrentScene.AddChild(orb);
        }

        QueueFree();
    }
}
