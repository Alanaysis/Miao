using Godot;
using Miao.Weapon;

namespace Miao.Player;

public partial class Hunter : Player
{
    [Export] public PackedScene ArrowScene;

    public override void _Ready()
    {
        MoveSpeed = 240;
        MaxHealth = 80;
        MaxShield = 0; // 猎人无被动护盾
        Skill1Cooldown = 8.0f;
        Skill2Cooldown = 15.0f;
        base._Ready();
    }

    protected override void UseSkill1()
    {
        // 闪现：短距离瞬移
        Skill1Timer = Skill1Cooldown;
        EmitSignal(SignalName.Skill1Used);

        var blinkDir = Velocity.Normalized();
        if (blinkDir == Vector2.Zero)
            blinkDir = GlobalTransform.X.Normalized();

        GlobalPosition += blinkDir * 120;

        // TODO: 添加无敌帧逻辑（0.3秒内 TakeDamage 不生效）
        // TODO: 闪现残影特效
    }

    protected override void UseSkill2()
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
}
