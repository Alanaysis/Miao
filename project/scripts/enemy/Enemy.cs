using Godot;
using Miao.Weapon;

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

    /// <summary>房间索引（用于掉落等级）</summary>
    [Export] public int RoomIndex { get; set; }

    /// <summary>是否为精英/Boss（影响掉落稀有度）</summary>
    [Export] public bool IsElite { get; set; }

    public int CurrentHealth { get; private set; }

    private Node2D _player;
    private float _damageCooldownTimer;
    private Vector2 _knockbackVelocity;
    private float _markMultiplier = 1.0f;
    private float _markTimer = 0;

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

        // 标记计时
        if (_markTimer > 0)
        {
            _markTimer -= (float)delta;
            if (_markTimer <= 0) _markMultiplier = 1.0f;
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

    public void ApplyMark(float multiplier, float duration)
    {
        _markMultiplier = multiplier;
        _markTimer = duration;
    }

    /// <summary>
    /// 对敌人造成伤害
    /// </summary>
    /// <param name="damage">伤害值</param>
    public void TakeDamage(int damage)
    {
        damage = Mathf.RoundToInt(damage * _markMultiplier);
        CurrentHealth -= damage;

        if (CurrentHealth <= 0)
        {
            CallDeferred(nameof(Die));
        }
    }

    private void Die()
    {
        EmitSignal(SignalName.EnemyDied, ExperienceValue);

        // 找到 player 并充能超能
        var players = GetTree().GetNodesInGroup("player");
        if (players.Count > 0 && players[0] is Miao.Player.Player p)
        {
            p.AddSuperCharge(p.SuperChargePerKill);
        }

        // 掉落经验球
        if (ExperienceOrbScene != null)
        {
            var orb = ExperienceOrbScene.Instantiate<Node2D>();
            orb.GlobalPosition = GlobalPosition;
            GetTree().CurrentScene.AddChild(orb);
        }

        // 掉落武器
        if (GD.Randf() < LootTable.BaseDropChance)
        {
            var weaponData = LootTable.GenerateWeapon(RoomIndex, IsElite);
            SpawnWeaponDrop(weaponData);
        }

        // 掉落微光（通过击杀和完成游戏获得）
        // (already handled by MetaProgression elsewhere)

        QueueFree();
    }

    private void SpawnWeaponDrop(WeaponData data)
    {
        var drop = new Area2D();
        drop.CollisionLayer = 4;
        drop.CollisionMask = 1;

        var shape = new CircleShape2D();
        shape.Radius = 16;
        var collision = new CollisionShape2D();
        collision.Shape = shape;
        drop.AddChild(collision);

        var color = data.Rarity switch
        {
            Rarity.Common => new Color(0.7f, 0.7f, 0.7f),
            Rarity.Uncommon => new Color(0.2f, 0.8f, 0.2f),
            Rarity.Rare => new Color(0.2f, 0.4f, 1.0f),
            Rarity.Epic => new Color(0.6f, 0.2f, 0.8f),
            Rarity.Legendary => new Color(1.0f, 0.8f, 0.0f),
            _ => Colors.White
        };

        var visual = new ColorRect();
        visual.Size = new Vector2(12, 12);
        visual.Position = new Vector2(-6, -6);
        visual.Color = color;
        drop.AddChild(visual);

        drop.GlobalPosition = GlobalPosition + new Vector2(GD.RandRange(-20, 20), GD.RandRange(-20, 20));

        drop.SetMeta("weapon_data", data);

        drop.BodyEntered += (body) =>
        {
            if (body is Miao.Player.Player player)
            {
                player.SetNearbyWeapon(data, drop);
            }
        };
        drop.BodyExited += (body) =>
        {
            if (body is Miao.Player.Player player)
            {
                player.ClearNearbyWeapon();
            }
        };

        GetTree().CurrentScene.AddChild(drop);
    }
}
