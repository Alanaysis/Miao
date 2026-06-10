using Godot;
using Miao.Weapon;

namespace Miao.Player;

public partial class Titan : Player
{
    protected override string GetClassName() => "titan";

    public override void _Ready()
    {
        MoveSpeed = 180;
        MaxHealth = 120;
        MaxShield = 20; // Titan has natural shield
        Skill1Cooldown = 8.0f;
        Skill2Cooldown = 15.0f;
        base._Ready();

        // Apply subclass cooldown configs
        ApplySubclassCooldowns();
    }

    private void ApplySubclassCooldowns()
    {
        if (Subclass?.ActiveConfig != null)
        {
            var skill1 = Subclass.GetSkill1Config();
            var skill2 = Subclass.GetSkill2Config();
            if (skill1 != null) Skill1Cooldown = skill1.Cooldown;
            if (skill2 != null) Skill2Cooldown = skill2.Cooldown;
        }
    }

    protected override void UseSkill1()
    {
        if (Subclass == null) return;

        switch (Subclass.ActiveSubclass)
        {
            case SubclassType.Solar:
                SolarCharge();
                break;
            case SubclassType.Arc:
                ArcThunderCharge();
                break;
            case SubclassType.Void:
                VoidPunch();
                break;
        }
    }

    protected override void UseSkill2()
    {
        if (Subclass == null) return;

        switch (Subclass.ActiveSubclass)
        {
            case SubclassType.Solar:
                SolarShield();
                break;
            case SubclassType.Arc:
                ArcBarrier();
                break;
            case SubclassType.Void:
                VoidBarrier();
                break;
        }
    }

    protected override void UseSuper()
    {
        if (Subclass == null) return;

        switch (Subclass.ActiveSubclass)
        {
            case SubclassType.Solar:
                SolarHammerStrike();
                break;
            case SubclassType.Arc:
                ArcThundercrash();
                break;
            case SubclassType.Void:
                VoidWardOfDawn();
                break;
        }
    }

    // === SOLAR SUBCLASS ===

    private void SolarCharge()
    {
        // 冲锋：向前冲刺，撞到敌人眩晕
        Skill1Timer = Skill1Cooldown;
        EmitSignal(SignalName.Skill1Used);

        var chargeDir = Velocity.Normalized();
        if (chargeDir == Vector2.Zero)
            chargeDir = GlobalTransform.X.Normalized();

        // Dash forward
        GlobalPosition += chargeDir * 150;

        // Damage and stun enemies in path
        foreach (var body in GetTree().GetNodesInGroup("enemy"))
        {
            if (body is Enemy.Enemy enemy)
            {
                float dist = GlobalPosition.DistanceTo(enemy.GlobalPosition);
                if (dist < 80)
                {
                    enemy.TakeDamage(25);
                    enemy.ApplyKnockback(chargeDir * 400);
                }
            }
        }
    }

    private void SolarShield()
    {
        // 护盾：生成护盾吸收伤害
        Skill2Timer = Skill2Cooldown;
        EmitSignal(SignalName.Skill2Used);
        Shield += 50;
        EmitSignal(SignalName.ShieldChanged, Shield, MaxShield);
    }

    private void SolarHammerStrike()
    {
        // 超能：烈日锤击 — 跳起砸地 AOE + 击退
        SuperCharge = 0;
        EmitSignal(SignalName.SuperChargeChanged, SuperCharge, SuperMaxCharge);
        EmitSignal(SignalName.SuperUsed);

        // AOE damage around player
        foreach (var body in GetTree().GetNodesInGroup("enemy"))
        {
            if (body is Enemy.Enemy enemy)
            {
                float dist = GlobalPosition.DistanceTo(enemy.GlobalPosition);
                if (dist < 200)
                {
                    enemy.TakeDamage(80);
                    enemy.ApplyKnockback(GlobalPosition.DirectionTo(enemy.GlobalPosition) * 500);
                }
            }
        }
    }

    // === ARC SUBCLASS ===

    private void ArcThunderCharge()
    {
        // 雷霆冲锋：高速冲刺+伤害
        Skill1Timer = Skill1Cooldown;
        EmitSignal(SignalName.Skill1Used);

        var chargeDir = Velocity.Normalized();
        if (chargeDir == Vector2.Zero)
            chargeDir = GlobalTransform.X.Normalized();

        GlobalPosition += chargeDir * 200;

        foreach (var body in GetTree().GetNodesInGroup("enemy"))
        {
            if (body is Enemy.Enemy enemy)
            {
                float dist = GlobalPosition.DistanceTo(enemy.GlobalPosition);
                if (dist < 100)
                {
                    enemy.TakeDamage(35);
                    enemy.ApplyKnockback(chargeDir * 300);
                }
            }
        }
    }

    private void ArcBarrier()
    {
        // 电弧屏障：电弧护盾
        Skill2Timer = Skill2Cooldown;
        EmitSignal(SignalName.Skill2Used);
        Shield += 40;
        EmitSignal(SignalName.ShieldChanged, Shield, MaxShield);
    }

    private void ArcThundercrash()
    {
        // 超能：雷神之怒 — 地面电弧波
        SuperCharge = 0;
        EmitSignal(SignalName.SuperChargeChanged, SuperCharge, SuperMaxCharge);
        EmitSignal(SignalName.SuperUsed);

        var mousePos = GetGlobalMousePosition();
        var dir = GlobalPosition.DirectionTo(mousePos);

        // Launch forward
        GlobalPosition += dir * 300;

        // AOE at landing
        foreach (var body in GetTree().GetNodesInGroup("enemy"))
        {
            if (body is Enemy.Enemy enemy)
            {
                float dist = GlobalPosition.DistanceTo(enemy.GlobalPosition);
                if (dist < 250)
                {
                    enemy.TakeDamage(100);
                    enemy.ApplyKnockback(GlobalPosition.DirectionTo(enemy.GlobalPosition) * 600);
                }
            }
        }
    }

    // === VOID SUBCLASS ===

    private void VoidPunch()
    {
        // 虚空之拳：近战AOE+减速
        Skill1Timer = Skill1Cooldown;
        EmitSignal(SignalName.Skill1Used);

        foreach (var body in GetTree().GetNodesInGroup("enemy"))
        {
            if (body is Enemy.Enemy enemy)
            {
                float dist = GlobalPosition.DistanceTo(enemy.GlobalPosition);
                if (dist < 80)
                {
                    enemy.TakeDamage(20);
                    enemy.ApplyKnockback(GlobalPosition.DirectionTo(enemy.GlobalPosition) * 200);
                }
            }
        }
    }

    private void VoidBarrier()
    {
        // 虚空屏障：吸收+反弹
        Skill2Timer = Skill2Cooldown;
        EmitSignal(SignalName.Skill2Used);
        Shield += 60;
        EmitSignal(SignalName.ShieldChanged, Shield, MaxShield);
    }

    private void VoidWardOfDawn()
    {
        // 超能：虚空守护者 — 团队护盾
        SuperCharge = 0;
        EmitSignal(SignalName.SuperChargeChanged, SuperCharge, SuperMaxCharge);
        EmitSignal(SignalName.SuperUsed);

        Shield += 100;
        EmitSignal(SignalName.ShieldChanged, Shield, MaxShield);
    }
}
