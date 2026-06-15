using Godot;
using System.Collections.Generic;
using Miao.Enemy;
using Miao.Network;

namespace Miao.Weapon;

public partial class Bullet : Area2D
{
	public Vector2 Velocity { get; set; }
	public int Damage { get; set; }
	public float Knockback { get; set; }
	public bool CanPenetrate { get; set; }
	public float Lifetime { get; set; } = 3.0f;

	private float _lifetime;
	private HashSet<Node2D> _hitTargets = new();

	public override void _Ready()
	{
		_lifetime = Lifetime;
		CollisionLayer = 0;
		CollisionMask = 2; // 敌人层

		var shape = new CircleShape2D();
		shape.Radius = 5;
		var collision = new CollisionShape2D();
		collision.Shape = shape;
		AddChild(collision);

		// 子弹视觉：小矩形 + 发光色
		var visual = new ColorRect();
		visual.Size = new Vector2(12, 4);
		visual.Position = new Vector2(-6, -2);
		visual.Color = new Color(1.0f, 0.9f, 0.3f); // 亮黄色
		AddChild(visual);

		// 旋转子弹朝向飞行方向
		Rotation = Velocity.Angle();

		BodyEntered += OnBodyEntered;
	}

	public override void _Process(double delta)
	{
		Position += Velocity * (float)delta;
		_lifetime -= (float)delta;
		if (_lifetime <= 0) QueueFree();
	}

	private void OnBodyEntered(Node2D body)
	{
		if (body is Enemy.Enemy enemy && !_hitTargets.Contains(body))
		{
			_hitTargets.Add(body);

			// 多人联机：伤害走主机验证
			if (Multiplayer.HasMultiplayerPeer() && !Multiplayer.IsServer())
			{
				// 客户端发送伤害请求给主机
				var enemySync = enemy.GetNodeOrNull<EnemySync>("EnemySync");
				if (enemySync != null)
				{
					int sourcePlayerId = (int)Multiplayer.GetUniqueId();
					enemySync.Rpc(nameof(EnemySync.RequestDamage), enemy.NetworkId, Damage, sourcePlayerId);
				}
			}
			else
			{
				// 单人或主机直接应用伤害
				enemy.TakeDamage(Damage);
			}

			enemy.ApplyKnockback(Velocity.Normalized() * Knockback);

			// 命中特效：小爆炸
			SpawnHitEffect(GlobalPosition);

			if (HasMeta("is_rocket"))
			{
				// Spawn AOE explosion
				var explosion = GD.Load<PackedScene>("res://scenes/weapon/RocketExplosion.tscn");
				if (explosion != null)
				{
					var boom = explosion.Instantiate<RocketExplosion>();
					boom.GlobalPosition = GlobalPosition;
					boom.Init(Damage, 80f);
					boom.SourcePlayerId = (int)Multiplayer.GetUniqueId();
					GetTree().CurrentScene.AddChild(boom);
				}
			}

			if (!CanPenetrate)
			{
				QueueFree();
			}
		}
	}

	private void SpawnHitEffect(Vector2 pos)
	{
		for (int i = 0; i < 4; i++)
		{
			var particle = new ColorRect();
			particle.Size = new Vector2(3, 3);
			particle.Position = pos - new Vector2(1.5f, 1.5f);
			particle.Color = new Color(1.0f, 0.5f, 0.1f);
			particle.ZIndex = 10;
			GetTree().CurrentScene.AddChild(particle);

			// 随机方向散开
			float angle = GD.Randf() * Mathf.Tau;
			float speed = 40 + GD.Randf() * 60;
			var vel = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;

			// 用 Tween 做散开+消失动画
			var tween = GetTree().CreateTween();
			tween.TweenProperty(particle, "position", particle.Position + vel, 0.2);
			tween.Parallel().TweenProperty(particle, "modulate:a", 0.0f, 0.2);
			tween.TweenCallback(Callable.From(() =>
			{
				if (IsInstanceValid(particle)) particle.QueueFree();
			}));
		}
	}
}
