using Godot;
using global::System.Collections.Generic;
using Miao.Armor;
using Miao.System;

namespace Miao.Weapon;

public partial class Weapon : Node2D
{
    [Export] public PackedScene BulletScene;

    public WeaponData Data { get; private set; }
    private float _cooldownTimer;

    // FusionRifle charge state
    private bool _isCharging;
    private float _chargeTimer;
    private const float FusionChargeTime = 0.8f;

    // Burst fire state (PulseRifle)
    private int _burstRemaining;
    private float _burstDelayTimer;
    private const float BurstDelay = 0.05f;

    // KillClip state
    private bool _killClipActive = false;
    private float _killClipTimer = 0;

    // Perk 修饰后的实际属性
    public float EffectiveFireRate
    {
        get
        {
            float baseRate = Data.FireRate * PerkSystem.GetFireRateMultiplier(Data);
            // Apply mod bonuses from armor
            float modBonus = GetModFireRateBonus();
            return baseRate * (1f + modBonus);
        }
    }
    public int EffectiveDamage
    {
        get
        {
            float codexBonus = CollectionCodex.Instance?.CollectionDamageBonus ?? 1.0f;
            float metaBonus = MetaProgression.Instance?.GetBonusDamage() ?? 1.0f;
            float modBonus = 1f + GetModDamageBonus();
            float lightMult = GetLightLevelMultiplier();
            float killClipMult = _killClipActive ? 1.3f : 1.0f;
            return Mathf.RoundToInt(Data.BaseDamage * PerkSystem.GetDamageMultiplier(Data) * codexBonus * metaBonus * modBonus * lightMult * killClipMult);
        }
    }
    public float EffectiveKnockback => Data.KnockbackForce * PerkSystem.GetKnockbackMultiplier(Data);

    public void SetWeaponData(WeaponData data)
    {
        Data = data;
        _cooldownTimer = 0;
        _isCharging = false;
        _chargeTimer = 0;
        _burstRemaining = 0;
    }

    public void ActivateKillClip()
    {
        _killClipActive = true;
        _killClipTimer = 5.0f; // 5 seconds
    }

    public override void _Process(double delta)
    {
        if (Data == null) return;

        float dt = (float)delta;
        _cooldownTimer -= dt;

        // Handle burst fire cooldown
        if (_burstRemaining > 0)
        {
            _burstDelayTimer -= dt;
            if (_burstDelayTimer <= 0)
            {
                _burstRemaining--;
                _burstDelayTimer = BurstDelay;
                var mousePos = GetGlobalMousePosition();
                var fireDirection = (mousePos - GlobalPosition).Normalized();
                FireBullet(fireDirection);
            }
        }

        // Handle FusionRifle charge
        if (_isCharging)
        {
            _chargeTimer -= dt;
            if (_chargeTimer <= 0)
            {
                // Charge complete -- fire 5 bolts
                _isCharging = false;
                FireFusionBolts();
            }
        }

        // KillClip timer
        if (_killClipActive)
        {
            _killClipTimer -= dt;
            if (_killClipTimer <= 0) _killClipActive = false;
        }
    }

    /// <summary>
    /// 由 Player 的 Shoot 信号调用（按下时）
    /// </summary>
    public void TryFire()
    {
        if (Data == null || _cooldownTimer > 0) return;

        switch (Data.Type)
        {
            case WeaponType.FusionRifle:
                StartFusionCharge();
                break;
            case WeaponType.Sword:
                FireSwordSlash();
                _cooldownTimer = 1.0f / EffectiveFireRate;
                break;
            default:
                FireWeapon();
                break;
        }
    }

    /// <summary>
    /// 由 Player 的 Shoot 信号调用（松开时）-- 用于 FusionRifle 取消蓄力
    /// </summary>
    public void TryReleaseFire()
    {
        if (Data == null) return;

        if (Data.Type == WeaponType.FusionRifle && _isCharging)
        {
            // Released before charge completes -- cancel, nothing fires
            _isCharging = false;
            _chargeTimer = 0;
        }
    }

    private void FireWeapon()
    {
        var mousePos = GetGlobalMousePosition();
        var fireDirection = (mousePos - GlobalPosition).Normalized();

        switch (Data.Type)
        {
            case WeaponType.PulseRifle:
                // 3-round burst
                _burstRemaining = 3;
                _burstDelayTimer = 0; // fire first bullet immediately
                FireBullet(fireDirection);
                _burstRemaining--; // first one fired
                _burstDelayTimer = BurstDelay;
                _cooldownTimer = 1.0f / EffectiveFireRate;
                break;

            case WeaponType.ScoutRifle:
                // Single shot, high damage, long cooldown, no spread
                FireBullet(fireDirection);
                _cooldownTimer = 1.0f / EffectiveFireRate;
                break;

            case WeaponType.SMG:
                // Very fast single shots, slight spread
                float smgSpread = Data.SpreadAngle * PerkSystem.GetSpreadMultiplier(Data);
                var smgDir = fireDirection.Rotated((GD.Randf() - 0.5f) * smgSpread);
                FireBullet(smgDir);
                _cooldownTimer = 1.0f / EffectiveFireRate;
                break;

            case WeaponType.SniperRifle:
                // Single shot, very high damage, no spread
                FireBullet(fireDirection);
                _cooldownTimer = 1.0f / EffectiveFireRate;
                break;

            case WeaponType.RocketLauncher:
                // Single shot -- spawns AOE on impact
                FireRocket(fireDirection);
                _cooldownTimer = 1.0f / EffectiveFireRate;
                break;

            default:
                // AutoRifle, HandCannon, Shotgun -- existing logic
                FireStandardWeapon(fireDirection);
                break;
        }
    }

    private void FireStandardWeapon(Vector2 fireDirection)
    {
        if (Data.BulletCount <= 1)
        {
            FireBullet(fireDirection);
        }
        else
        {
            // Shotgun: multi-bullet spread
            float totalSpread = Data.SpreadAngle * PerkSystem.GetSpreadMultiplier(Data);
            float step = totalSpread / (Data.BulletCount - 1);
            float startAngle = -totalSpread / 2;

            for (int i = 0; i < Data.BulletCount; i++)
            {
                float offset = startAngle + step * i;
                var dir = fireDirection.Rotated(offset);
                FireBullet(dir);
            }
        }

        _cooldownTimer = 1.0f / EffectiveFireRate;
    }

    // --- FusionRifle ---

    private void StartFusionCharge()
    {
        _isCharging = true;
        _chargeTimer = FusionChargeTime;
        _cooldownTimer = 1.0f / EffectiveFireRate;
    }

    private void FireFusionBolts()
    {
        var mousePos = GetGlobalMousePosition();
        var fireDirection = (mousePos - GlobalPosition).Normalized();

        // 5 bolts in a spread
        float totalSpread = Data.SpreadAngle;
        float step = totalSpread / 4; // 5 bolts = 4 gaps
        float startAngle = -totalSpread / 2;

        for (int i = 0; i < 5; i++)
        {
            float offset = startAngle + step * i;
            var dir = fireDirection.Rotated(offset);
            FireBullet(dir);
        }
    }

    // --- RocketLauncher ---

    private void FireRocket(Vector2 direction)
    {
        if (BulletScene == null) return;

        var bullet = BulletScene.Instantiate<Bullet>();
        bullet.GlobalPosition = GlobalPosition;
        bullet.Velocity = direction * Data.BulletSpeed;
        bullet.Damage = EffectiveDamage;
        bullet.Knockback = EffectiveKnockback;
        bullet.CanPenetrate = false;
        // Store AOE radius for on-impact explosion
        bullet.SetMeta("aoe_radius", 80.0f);
        bullet.SetMeta("is_rocket", true);
        GetTree().CurrentScene.AddChild(bullet);

        SpawnMuzzleFlash(direction);
    }

    // --- Sword ---

    private void FireSwordSlash()
    {
        var scene = GetTree().CurrentScene;
        var slash = new SwordSlash();
        slash.GlobalPosition = GlobalPosition;

        var mousePos = GetGlobalMousePosition();
        var dir = (mousePos - GlobalPosition).Normalized();
        // Offset the slash area in front of the player
        slash.GlobalPosition += dir * 40;
        slash.Rotation = dir.Angle();

        slash.Init(EffectiveDamage, EffectiveKnockback);
        scene.AddChild(slash);
    }

    // --- Shared ---

    private void FireBullet(Vector2 direction)
    {
        if (BulletScene == null) return;

        var bullet = BulletScene.Instantiate<Bullet>();
        bullet.GlobalPosition = GlobalPosition;
        bullet.Velocity = direction * Data.BulletSpeed;
        bullet.Damage = EffectiveDamage;
        bullet.Knockback = EffectiveKnockback;
        bullet.CanPenetrate = PerkSystem.HasPerk(Data, PerkId.Penetration);
        GetTree().CurrentScene.AddChild(bullet);

        // 枪口闪光特效
        SpawnMuzzleFlash(direction);
    }

    private void SpawnMuzzleFlash(Vector2 direction)
    {
        var flash = new ColorRect();
        flash.Size = new Vector2(12, 6);
        flash.Position = GlobalPosition + direction * 16 - new Vector2(6, 3);
        flash.Color = new Color(1.0f, 0.8f, 0.2f, 0.9f);
        flash.Rotation = direction.Angle();
        flash.ZIndex = 10;
        GetTree().CurrentScene.AddChild(flash);

        // 0.06秒后消失
        GetTree().CreateTimer(0.06).Timeout += () =>
        {
            if (IsInstanceValid(flash)) flash.QueueFree();
        };
    }

    /// <summary>
    /// 获取光等对伤害的倍率
    /// </summary>
    private float GetLightLevelMultiplier()
    {
        var node = GetParent();
        while (node != null)
        {
            if (node is Miao.Player.Player player)
            {
                int totalLight = LightLevelCalculator.CalculateTotalLightLevel(Data, player.Armors);
                return LightLevelCalculator.GetDamageMultiplier(totalLight);
            }
            node = node.GetParent();
        }
        return 1.0f;
    }

    /// <summary>
    /// 获取玩家护甲模组的伤害加成
    /// </summary>
    private float GetModDamageBonus()
    {
        var armorMods = GetPlayerMods();
        if (armorMods == null) return 0f;
        return ModEffectProcessor.GetDamageBonus(armorMods);
    }

    /// <summary>
    /// 获取玩家护甲模组的射速加成
    /// </summary>
    private float GetModFireRateBonus()
    {
        var armorMods = GetPlayerMods();
        if (armorMods == null) return 0f;
        return ModEffectProcessor.GetFireRateBonus(armorMods);
    }

    /// <summary>
    /// 获取玩家装备的所有模组
    /// </summary>
    private List<ModData> GetPlayerMods()
    {
        // 遍历父节点找到 Player
        var node = GetParent();
        while (node != null)
        {
            if (node is Miao.Player.Player player)
            {
                return GetEquippedMods(player.Armors);
            }
            node = node.GetParent();
        }
        return null;
    }

    /// <summary>
    /// 从 ArmorManager 获取所有已装备的模组数据
    /// </summary>
    private List<ModData> GetEquippedMods(ArmorManager armorManager)
    {
        if (armorManager == null) return null;

        var mods = new List<ModData>();
        foreach (var slot in armorManager.Slots.Values)
        {
            if (slot.EquippedArmor?.EquippedMods != null)
            {
                // TODO: Load actual ModData from mod IDs when mod loading is implemented
                // For now, return empty list
            }
        }
        return mods;
    }
}
