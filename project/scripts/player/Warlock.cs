using Godot;
using Miao.Weapon;

namespace Miao.Player;

public partial class Warlock : Player
{
    protected override string GetClassName() => "warlock";

    public override void _Ready()
    {
        MoveSpeed = 200;
        MaxHealth = 90;
        MaxShield = 0;
        Skill1Cooldown = 15.0f; // Longer cooldowns but stronger effects
        Skill2Cooldown = 12.0f;
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
            case SubclassType.Arc:
                ArcHealingRift();
                break;
            case SubclassType.Void:
                VoidGrenade();
                break;
            case SubclassType.Solar:
                SolarFireTouch();
                break;
        }
    }

    protected override void UseSkill2()
    {
        if (Subclass == null) return;

        switch (Subclass.ActiveSubclass)
        {
            case SubclassType.Arc:
                ArcEmpower();
                break;
            case SubclassType.Void:
                VoidBlink();
                break;
            case SubclassType.Solar:
                SolarHeatWave();
                break;
        }
    }

    protected override void UseSuper()
    {
        if (Subclass == null) return;

        switch (Subclass.ActiveSubclass)
        {
            case SubclassType.Arc:
                ArcStormtrance();
                break;
            case SubclassType.Void:
                VoidNovaBomb();
                break;
            case SubclassType.Solar:
                SolarDawnBlade();
                break;
        }
    }

    // === ARC SUBCLASS ===

    private void ArcHealingRift()
    {
        // Healing rift: place a healing zone that restores HP over time
        Skill1Timer = Skill1Cooldown;
        EmitSignal(SignalName.Skill1Used);

        // Create healing zone
        var rift = new Area2D();
        rift.CollisionLayer = 0;
        rift.CollisionMask = 1; // Player layer

        var shape = new CircleShape2D();
        shape.Radius = 80;
        var collision = new CollisionShape2D();
        collision.Shape = shape;
        rift.AddChild(collision);

        // Visual
        var visual = new ColorRect();
        visual.Size = new Vector2(160, 160);
        visual.Position = new Vector2(-80, -80);
        visual.Color = new Color(0.2f, 0.8f, 0.2f, 0.3f);
        rift.AddChild(visual);

        rift.GlobalPosition = GlobalPosition;

        float duration = 8.0f;
        float healPerTick = 5.0f;
        float tickInterval = 0.5f;

        rift.ProcessMode = ProcessModeEnum.Pausable;
        rift.SetProcess(true);

        GetTree().CurrentScene.AddChild(rift);

        // Use a tween for the heal ticks
        var tween = CreateTween();
        tween.TweenCallback(Callable.From(() =>
        {
            foreach (var body in rift.GetOverlappingBodies())
            {
                if (body is Player player)
                {
                    player.Heal(Mathf.RoundToInt(healPerTick));
                }
            }
        })).SetDelay(tickInterval);
        tween.SetLoops(Mathf.RoundToInt(duration / tickInterval));

        // Remove after duration
        GetTree().CreateTimer(duration).Timeout += () =>
        {
            if (IsInstanceValid(rift)) rift.QueueFree();
        };
    }

    private void ArcEmpower()
    {
        // Empower: increase fire rate and damage by 50%
        Skill2Timer = Skill2Cooldown;
        EmitSignal(SignalName.Skill2Used);

        // TODO: implement buff system for fire rate / damage increase
    }

    private void ArcStormtrance()
    {
        // Super: Stormtrance - continuous arc damage around the player
        SuperCharge = 0;
        EmitSignal(SignalName.SuperChargeChanged, SuperCharge, SuperMaxCharge);
        EmitSignal(SignalName.SuperUsed);

        float duration = 5.0f;
        float damagePerTick = 15.0f;
        float tickInterval = 0.3f;

        var tween = CreateTween();
        tween.TweenCallback(Callable.From(() =>
        {
            foreach (var node in GetTree().GetNodesInGroup("enemy"))
            {
                if (node is Enemy.Enemy enemy)
                {
                    float dist = GlobalPosition.DistanceTo(enemy.GlobalPosition);
                    if (dist < 150)
                    {
                        enemy.TakeDamage(Mathf.RoundToInt(damagePerTick));
                    }
                }
            }
        })).SetDelay(tickInterval);
        tween.SetLoops(Mathf.RoundToInt(duration / tickInterval));
    }

    // === VOID SUBCLASS ===

    private void VoidGrenade()
    {
        // Void grenade: tracking + explosion
        Skill1Timer = Skill1Cooldown;
        EmitSignal(SignalName.Skill1Used);

        var mousePos = GetGlobalMousePosition();

        // Find nearest enemy to mouse
        Enemy.Enemy nearest = null;
        float nearestDist = 300f;
        foreach (var node in GetTree().GetNodesInGroup("enemy"))
        {
            if (node is Enemy.Enemy enemy)
            {
                float dist = mousePos.DistanceTo(enemy.GlobalPosition);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = enemy;
                }
            }
        }

        if (nearest != null)
        {
            // Create tracking projectile
            var bullet = GD.Load<PackedScene>("res://scenes/weapon/Bullet.tscn").Instantiate<Bullet>();
            bullet.GlobalPosition = GlobalPosition;
            bullet.Velocity = GlobalPosition.DirectionTo(nearest.GlobalPosition) * 400;
            bullet.Damage = 40;
            bullet.CanPenetrate = false;
            GetTree().CurrentScene.AddChild(bullet);
        }
    }

    private void VoidBlink()
    {
        // Void blink: short-range teleport
        Skill2Timer = Skill2Cooldown;
        EmitSignal(SignalName.Skill2Used);

        var blinkDir = Velocity.Normalized();
        if (blinkDir == Vector2.Zero)
            blinkDir = GlobalTransform.X.Normalized();

        GlobalPosition += blinkDir * 150;
    }

    private void VoidNovaBomb()
    {
        // Super: Nova Bomb - vortex at mouse position that pulls and damages enemies
        SuperCharge = 0;
        EmitSignal(SignalName.SuperChargeChanged, SuperCharge, SuperMaxCharge);
        EmitSignal(SignalName.SuperUsed);

        var mousePos = GetGlobalMousePosition();

        // Create vortex at mouse position
        var vortex = new Area2D();
        vortex.CollisionLayer = 0;
        vortex.CollisionMask = 2; // Enemy layer

        var shape = new CircleShape2D();
        shape.Radius = 120;
        var collision = new CollisionShape2D();
        collision.Shape = shape;
        vortex.AddChild(collision);

        var visual = new ColorRect();
        visual.Size = new Vector2(240, 240);
        visual.Position = new Vector2(-120, -120);
        visual.Color = new Color(0.5f, 0, 0.8f, 0.4f);
        vortex.AddChild(visual);

        vortex.GlobalPosition = mousePos;
        GetTree().CurrentScene.AddChild(vortex);

        float duration = 4.0f;
        float damagePerTick = 20.0f;
        float pullForce = 200f;

        var tween = CreateTween();
        tween.TweenCallback(Callable.From(() =>
        {
            foreach (var body in vortex.GetOverlappingBodies())
            {
                if (body is Enemy.Enemy enemy)
                {
                    enemy.TakeDamage(Mathf.RoundToInt(damagePerTick));
                    // Pull toward center
                    var pullDir = vortex.GlobalPosition.DirectionTo(enemy.GlobalPosition) * -1;
                    enemy.ApplyKnockback(pullDir * pullForce * 0.3f);
                }
            }
        })).SetDelay(0.5f);
        tween.SetLoops(Mathf.RoundToInt(duration / 0.5f));

        GetTree().CreateTimer(duration).Timeout += () =>
        {
            if (IsInstanceValid(vortex)) vortex.QueueFree();
        };
    }

    // === SOLAR SUBCLASS ===

    private void SolarFireTouch()
    {
        // Fire touch: melee ignite nearby enemies
        Skill1Timer = Skill1Cooldown;
        EmitSignal(SignalName.Skill1Used);

        foreach (var node in GetTree().GetNodesInGroup("enemy"))
        {
            if (node is Enemy.Enemy enemy)
            {
                float dist = GlobalPosition.DistanceTo(enemy.GlobalPosition);
                if (dist < 70)
                {
                    enemy.TakeDamage(30);
                    // TODO: apply burn DOT effect
                }
            }
        }
    }

    private void SolarHeatWave()
    {
        // Heat wave: fire wave projectiles in a spread
        Skill2Timer = Skill2Cooldown;
        EmitSignal(SignalName.Skill2Used);

        var fireDir = GlobalTransform.X.Normalized();

        // Create fire wave projectile
        for (int i = -1; i <= 1; i++)
        {
            var dir = fireDir.Rotated(i * 0.2f);
            var bullet = GD.Load<PackedScene>("res://scenes/weapon/Bullet.tscn").Instantiate<Bullet>();
            bullet.GlobalPosition = GlobalPosition;
            bullet.Velocity = dir * 350;
            bullet.Damage = 25;
            bullet.CanPenetrate = true;
            bullet.Lifetime = 1.5f;
            GetTree().CurrentScene.AddChild(bullet);
        }
    }

    private void SolarDawnBlade()
    {
        // Super: Dawnblade - summon flame sword, continuous melee AOE
        SuperCharge = 0;
        EmitSignal(SignalName.SuperChargeChanged, SuperCharge, SuperMaxCharge);
        EmitSignal(SignalName.SuperUsed);

        float duration = 6.0f;
        float damagePerSwing = 50.0f;
        float swingInterval = 0.5f;

        var tween = CreateTween();
        tween.TweenCallback(Callable.From(() =>
        {
            foreach (var node in GetTree().GetNodesInGroup("enemy"))
            {
                if (node is Enemy.Enemy enemy)
                {
                    float dist = GlobalPosition.DistanceTo(enemy.GlobalPosition);
                    if (dist < 120)
                    {
                        enemy.TakeDamage(Mathf.RoundToInt(damagePerSwing));
                        enemy.ApplyKnockback(GlobalPosition.DirectionTo(enemy.GlobalPosition) * 300);
                    }
                }
            }
        })).SetDelay(swingInterval);
        tween.SetLoops(Mathf.RoundToInt(duration / swingInterval));
    }
}
