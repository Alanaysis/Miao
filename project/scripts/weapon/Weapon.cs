using Godot;
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

    // Perk 修饰后的实际属性
    public float EffectiveFireRate => Data.FireRate * PerkSystem.GetFireRateMultiplier(Data);
    public int EffectiveDamage
    {
        get
        {
            float codexBonus = CollectionCodex.Instance?.CollectionDamageBonus ?? 1.0f;
            float metaBonus = MetaProgression.Instance?.GetBonusDamage() ?? 1.0f;
            return Mathf.RoundToInt(Data.BaseDamage * PerkSystem.GetDamageMultiplier(Data) * codexBonus * metaBonus);
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
}
