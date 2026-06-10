using Godot;
using System.Collections.Generic;
using Miao.Weapon;

namespace Miao.Player;

public partial class Hunter : Player
{
    [Export] public PackedScene ArrowScene;

    private ArcRollEffect _arcRollEffect;

    public override void _Ready()
    {
        MoveSpeed = 240;
        MaxHealth = 80;
        MaxShield = 0; // 猎人无被动护盾
        Skill1Cooldown = 8.0f;
        Skill2Cooldown = 15.0f;
        base._Ready();

        // 初始化电弧翻滚效果节点
        _arcRollEffect = new ArcRollEffect();
        AddChild(_arcRollEffect);

        // 应用子职业冷却时间配置
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

    public override void _Process(double delta)
    {
        base._Process(delta);
    }

    protected override void UseSkill1()
    {
        if (Subclass == null) return;

        switch (Subclass.ActiveSubclass)
        {
            case SubclassType.Void:
                UseVoidSkill1();
                break;
            case SubclassType.Arc:
                UseArcSkill1();
                break;
            case SubclassType.Solar:
                UseSolarSkill1();
                break;
        }
    }

    protected override void UseSkill2()
    {
        if (Subclass == null) return;

        switch (Subclass.ActiveSubclass)
        {
            case SubclassType.Void:
                UseVoidSkill2();
                break;
            case SubclassType.Arc:
                UseArcSkill2();
                break;
            case SubclassType.Solar:
                UseSolarSkill2();
                break;
        }
    }

    // === Void Subclass (虚空) ===

    private void UseVoidSkill1()
    {
        // 闪现：短距离瞬移
        Skill1Timer = Skill1Cooldown;
        EmitSignal(SignalName.Skill1Used);

        var blinkDir = Velocity.Normalized();
        if (blinkDir == Vector2.Zero)
            blinkDir = GlobalTransform.X.Normalized();

        GlobalPosition += blinkDir * 120;
    }

    private void UseVoidSkill2()
    {
        // 标记：标记鼠标位置最近的敌人，受伤害+20%
        Skill2Timer = Skill2Cooldown;
        EmitSignal(SignalName.Skill2Used);

        var mousePos = GetGlobalMousePosition();
        Enemy.Enemy nearest = null;
        float nearestDist = 200f;

        foreach (var node in GetTree().GetNodesInGroup("enemy"))
        {
            if (node is Enemy.Enemy enemy)
            {
                float dist = enemy.GlobalPosition.DistanceTo(mousePos);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = enemy;
                }
            }
        }

        if (nearest != null)
        {
            nearest.ApplyMark(1.2f, 10.0f);
        }
    }

    // === Arc Subclass (电弧) ===

    private void UseArcSkill1()
    {
        // 翻滚：快速位移 + 0.5秒无敌
        Skill1Timer = Skill1Cooldown;
        EmitSignal(SignalName.Skill1Used);

        var rollDir = Velocity.Normalized();
        if (rollDir == Vector2.Zero)
            rollDir = GlobalTransform.X.Normalized();

        GlobalPosition += rollDir * 120;
        _arcRollEffect.Apply(this);
    }

    private void UseArcSkill2()
    {
        // 雷击：对150px范围内所有敌人造成20伤害
        Skill2Timer = Skill2Cooldown;
        EmitSignal(SignalName.Skill2Used);

        float range = 150f;
        int damage = 20;

        foreach (var node in GetTree().GetNodesInGroup("enemy"))
        {
            if (node is Enemy.Enemy enemy)
            {
                float dist = enemy.GlobalPosition.DistanceTo(GlobalPosition);
                if (dist <= range)
                {
                    enemy.TakeDamage(damage);
                }
            }
        }
    }

    // === Solar Subclass (烈日) ===

    private void UseSolarSkill1()
    {
        // 火焰冲刺：冲刺150px，留下火焰轨迹
        Skill1Timer = Skill1Cooldown;
        EmitSignal(SignalName.Skill1Used);

        var dashDir = Velocity.Normalized();
        if (dashDir == Vector2.Zero)
            dashDir = GlobalTransform.X.Normalized();

        // 留下火焰轨迹（每30px一个）
        int trailCount = 5;
        for (int i = 0; i < trailCount; i++)
        {
            var trail = new SolarFireTrail();
            trail.GlobalPosition = GlobalPosition + dashDir * (30 * i);
            GetTree().CurrentScene.AddChild(trail);
        }

        GlobalPosition += dashDir * 150;
    }

    private void UseSolarSkill2()
    {
        // 燃烧弹：投掷爆炸物，造成30伤害 + 灼烧
        Skill2Timer = Skill2Cooldown;
        EmitSignal(SignalName.Skill2Used);

        var mousePos = GetGlobalMousePosition();
        float explosionRadius = 80f;
        int directDamage = 30;

        // 对爆炸范围内敌人造成直接伤害
        foreach (var node in GetTree().GetNodesInGroup("enemy"))
        {
            if (node is Enemy.Enemy enemy)
            {
                float dist = enemy.GlobalPosition.DistanceTo(mousePos);
                if (dist <= explosionRadius)
                {
                    enemy.TakeDamage(directDamage);
                    // TODO: 施加灼烧状态（10伤害/秒，持续3秒）
                }
            }
        }
    }

    protected override void UseSuper()
    {
        // 虚空箭雨：扇形射出大量箭矢，穿透所有敌人
        SuperCharge = 0;
        EmitSignal(SignalName.SuperChargeChanged, SuperCharge, SuperMaxCharge);
        EmitSignal(SignalName.SuperUsed);

        int arrowCount = 20;
        float totalSpread = Mathf.DegToRad(60);
        float step = totalSpread / (arrowCount - 1);
        float startAngle = -totalSpread / 2;
        var fireDir = GlobalTransform.X.Normalized();

        for (int i = 0; i < arrowCount; i++)
        {
            float offset = startAngle + step * i;
            var dir = fireDir.Rotated(offset);

            var bullet = ArrowScene.Instantiate<Bullet>();
            bullet.GlobalPosition = GlobalPosition;
            bullet.Velocity = dir * 600;
            bullet.Damage = 30;
            bullet.CanPenetrate = true;
            bullet.Lifetime = 2.0f;
            GetTree().CurrentScene.AddChild(bullet);
        }
    }

    // 公开方法，用于检查无敌状态（供外部调用）
    public bool IsInvincible()
    {
        return _arcRollEffect?.IsInvincible() ?? false;
    }
}
