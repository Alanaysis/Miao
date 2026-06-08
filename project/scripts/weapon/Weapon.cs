using Godot;
using Miao.System;

namespace Miao.Weapon;

public partial class Weapon : Node2D
{
    [Export] public PackedScene BulletScene;

    public WeaponData Data { get; private set; }
    private float _cooldownTimer;

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
    }

    public override void _Process(double delta)
    {
        if (Data == null) return;
        _cooldownTimer -= (float)delta;
    }

    /// <summary>
    /// 由 Player 的 Shoot 信号调用
    /// </summary>
    public void TryFire()
    {
        if (Data == null || _cooldownTimer > 0) return;

        _cooldownTimer = 1.0f / EffectiveFireRate;

        // 用从武器指向鼠标的方向，比 GlobalTransform.X 更准确
        var mousePos = GetGlobalMousePosition();
        var fireDirection = (mousePos - GlobalPosition).Normalized();

        if (Data.BulletCount <= 1)
        {
            FireBullet(fireDirection);
        }
        else
        {
            // 霰弹枪：多发子弹扇形散射
            float totalSpread = Data.SpreadAngle;
            float step = totalSpread / (Data.BulletCount - 1);
            float startAngle = -totalSpread / 2;

            for (int i = 0; i < Data.BulletCount; i++)
            {
                float offset = startAngle + step * i;
                var dir = fireDirection.Rotated(offset);
                FireBullet(dir);
            }
        }
    }

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
