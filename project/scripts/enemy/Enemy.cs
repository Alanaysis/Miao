using Godot;
using Miao.Armor;
using Miao.Network;
using Miao.Pickup;
using Miao.System;
using Miao.Weapon;

namespace Miao.Enemy;

/// <summary>
/// 敌人基类
/// 负责向玩家移动、受伤、死亡、掉落经验
/// </summary>
public partial class Enemy : CharacterBody2D
{
    /// <summary>网络 ID，用于跨网络标识敌人</summary>
    public int NetworkId { get; set; }
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

    /// <summary>敌人光等（影响受到的伤害）</summary>
    [Export] public int LightLevel { get; set; } = 10;

    public int CurrentHealth { get; private set; }

    private Node2D _player;
    private float _damageCooldownTimer;
    private Vector2 _knockbackVelocity;
    private float _markMultiplier = 1.0f;
    private float _markTimer = 0;
    private float _shield;

    // 视觉
    private ColorRect _hpBarBg;
    private ColorRect _hpBarFill;
    private float _hpBarWidth = 30;

    [Signal]
    public delegate void EnemyDiedEventHandler(int experienceValue);

    public override void _Ready()
    {
        CurrentHealth = MaxHealth;
        _player = GetTree().GetFirstNodeInGroup("player") as Node2D;
        AddToGroup("enemy");

        SetupVisuals();

        // Attach EnemySync for multiplayer state synchronization
        if (Multiplayer.HasMultiplayerPeer())
        {
            var sync = new EnemySync();
            sync.SetNetworkId(NetworkId);
            AddChild(sync);
        }
    }

    /// <summary>
    /// Allow network layer to update health on clients.
    /// </summary>
    public void SetNetworkHealth(int health)
    {
        CurrentHealth = Mathf.Clamp(health, 0, MaxHealth);
        UpdateHpBar();
    }

    private void SetupVisuals()
    {
        // 根据敌人类型设置颜色
        var sprite = GetNodeOrNull<Sprite2D>("Sprite2D");
        if (sprite != null)
        {
            if (this is FastBug) sprite.Modulate = new Color(0.6f, 0.8f, 1.2f); // 蓝色调
            else if (this is TankBug) sprite.Modulate = new Color(1.2f, 0.7f, 0.6f); // 红色调
            else if (this is SmallBug) sprite.Modulate = new Color(0.8f, 1.1f, 0.7f); // 绿色调
        }

        // 精英光环
        if (IsElite)
        {
            var glow = new ColorRect();
            glow.Size = new Vector2(40, 40);
            glow.Position = new Vector2(-20, -20);
            glow.Color = new Color(1, 0.9f, 0.2f, 0.25f);
            glow.ZIndex = -1;
            AddChild(glow);
        }

        // 血条（头顶）
        _hpBarBg = new ColorRect();
        _hpBarBg.Size = new Vector2(_hpBarWidth, 3);
        _hpBarBg.Position = new Vector2(-_hpBarWidth / 2, -24);
        _hpBarBg.Color = new Color(0.2f, 0.2f, 0.2f, 0.7f);
        AddChild(_hpBarBg);

        _hpBarFill = new ColorRect();
        _hpBarFill.Size = new Vector2(_hpBarWidth, 3);
        _hpBarFill.Position = new Vector2(-_hpBarWidth / 2, -24);
        _hpBarFill.Color = new Color(0.2f, 0.8f, 0.2f);
        AddChild(_hpBarFill);
    }

    private void UpdateHpBar()
    {
        if (_hpBarFill == null) return;
        float ratio = MaxHealth > 0 ? (float)CurrentHealth / MaxHealth : 0;
        _hpBarFill.Size = new Vector2(_hpBarWidth * ratio, 3);

        if (ratio > 0.6f) _hpBarFill.Color = new Color(0.2f, 0.8f, 0.2f);
        else if (ratio > 0.3f) _hpBarFill.Color = new Color(0.9f, 0.8f, 0.1f);
        else _hpBarFill.Color = new Color(0.9f, 0.2f, 0.1f);
    }

    public override void _PhysicsProcess(double delta)
    {
        // Only host runs enemy AI in multiplayer
        if (!Multiplayer.IsServer()) return;

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

    public void AddShield(float amount)
    {
        _shield += amount;
    }

    public void Heal(int amount)
    {
        CurrentHealth = Mathf.Min(CurrentHealth + amount, MaxHealth);
    }

    /// <summary>
    /// 对敌人造成伤害
    /// </summary>
    /// <param name="damage">伤害值</param>
    public void TakeDamage(int damage)
    {
        // 先扣护盾
        if (_shield > 0)
        {
            float absorbed = Mathf.Min(_shield, damage);
            _shield -= absorbed;
            damage -= Mathf.RoundToInt(absorbed);
        }

        // 应用标记加成
        damage = Mathf.RoundToInt(damage * _markMultiplier);

        // 应用光等对伤害的加成（基于玩家光等）
        var player = GetTree().GetFirstNodeInGroup("player") as Miao.Player.Player;
        if (player != null && player.Equipment?.CurrentWeapon?.Data != null)
        {
            int playerLight = LightLevelCalculator.CalculateTotalLightLevel(
                player.Equipment.CurrentWeapon.Data, player.Armors);
            float lightMult = LightLevelCalculator.GetDamageMultiplier(playerLight, LightLevel);
            damage = Mathf.RoundToInt(damage * lightMult);
        }

        CurrentHealth -= damage;
        CurrentHealth = Mathf.Max(CurrentHealth, 0);
        UpdateHpBar();

        if (CurrentHealth <= 0)
        {
            CallDeferred(nameof(Die));
        }
    }

    private void Die()
    {
        // Notify clients of enemy death before freeing
        if (Multiplayer.IsServer())
        {
            var sync = GetNodeOrNull<EnemySync>("EnemySync");
            sync?.Rpc(nameof(EnemySync.NotifyEnemyDeath), NetworkId);
        }

        EmitSignal(SignalName.EnemyDied, ExperienceValue);

        // 找到 player 并充能超能
        var players = GetTree().GetNodesInGroup("player");
        if (players.Count > 0 && players[0] is Miao.Player.Player p)
        {
            p.AddSuperCharge(p.SuperChargePerKill);

            // 应用模组的击杀回血效果
            ApplyHealOnKillMod(p);

            // Apply KillClip/KillReturn perks
            var weapon = p.Equipment?.CurrentWeapon;
            if (weapon?.Data != null)
            {
                if (PerkSystem.HasPerk(weapon.Data, PerkId.KillClip))
                {
                    // KillClip: next magazine +30% damage (mark as active)
                    weapon.ActivateKillClip();
                }
                if (PerkSystem.HasPerk(weapon.Data, PerkId.KillReturn))
                {
                    // KillReturn: simplified -- no ammo system yet, placeholder
                }
            }
        }

        // 掉落经验球
        if (ExperienceOrbScene != null)
        {
            var orb = ExperienceOrbScene.Instantiate<Node2D>();
            orb.GlobalPosition = GlobalPosition;
            GetTree().CurrentScene.AddChild(orb);
        }

        // 掉落武器或记忆水晶
        float dropRoll = GD.Randf();
        if (dropRoll < 0.05f) // 5% 掉落记忆水晶
        {
            var weaponData = LootTable.GenerateWeapon(RoomIndex, IsElite);
            SpawnEngramDrop(weaponData, weaponData.Rarity);
        }
        else if (dropRoll < 0.05f + LootTable.BaseDropChance)
        {
            var weaponData = LootTable.GenerateWeapon(RoomIndex, IsElite);
            GameManager.Instance.SpawnWeaponDrop(GlobalPosition, weaponData);
        }

        // 掉落微光（通过击杀和完成游戏获得）
        // (already handled by MetaProgression elsewhere)

        // 精英分裂
        var eliteMod = GetNodeOrNull<EliteModifier>("EliteModifier");
        eliteMod?.OnDeath();

        QueueFree();
    }

    /// <summary>
    /// 应用模组的击杀回血效果
    /// </summary>
    private void ApplyHealOnKillMod(Miao.Player.Player player)
    {
        if (player.Armors == null) return;

        // 获取所有已装备的模组
        var mods = new global::System.Collections.Generic.List<ModData>();
        foreach (var slot in player.Armors.Slots.Values)
        {
            if (slot.EquippedArmor?.EquippedMods != null)
            {
                // TODO: Load actual ModData from mod IDs when mod loading is implemented
            }
        }

        // 计算击杀回血量
        float healAmount = ModEffectProcessor.GetStatBonus(mods, "heal_on_kill");
        if (healAmount > 0)
        {
            player.Heal(Mathf.RoundToInt(healAmount));
        }
    }

    /// <summary>
    /// 生成记忆水晶掉落物
    /// </summary>
    protected void SpawnEngramDrop(WeaponData weaponData, Rarity rarity)
    {
        var engram = new MemoryEngram();
        engram.GlobalPosition = GlobalPosition + new Vector2(GD.RandRange(-20, 20), GD.RandRange(-20, 20));
        engram.Init(weaponData, rarity);
        GetTree().CurrentScene.AddChild(engram);
    }
}
