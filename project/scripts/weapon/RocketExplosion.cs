using Godot;
using Miao.Network;

namespace Miao.Weapon;

public partial class RocketExplosion : Area2D
{
    private int _damage;
    private float _radius;
    private float _lifetime = 0.3f;

    /// <summary>
    /// 发射此火箭的玩家 ID，用于自伤判断
    /// </summary>
    public int SourcePlayerId { get; set; }

    public void Init(int damage, float radius)
    {
        _damage = damage;
        _radius = radius;
    }

    public override void _Ready()
    {
        CollisionLayer = 0;
        CollisionMask = 2 | 1; // 敌人层 + 玩家层（用于自伤）

        var shape = new CircleShape2D();
        shape.Radius = _radius;
        var collision = new CollisionShape2D();
        collision.Shape = shape;
        AddChild(collision);

        // Visual explosion
        var circle = new ColorRect();
        circle.Size = new Vector2(_radius * 2, _radius * 2);
        circle.Position = new Vector2(-_radius, -_radius);
        circle.Color = new Color(1, 0.5f, 0, 0.6f);
        AddChild(circle);

        // 伤害范围内所有目标（仅主机执行）
        if (!Multiplayer.HasMultiplayerPeer() || Multiplayer.IsServer())
        {
            foreach (var body in GetOverlappingBodies())
            {
                if (body is Enemy.Enemy enemy)
                {
                    enemy.TakeDamage(_damage);
                }
                // 自伤：对发射者本人造成 50% 伤害，不伤害其他玩家
                else if (body is Miao.Player.Player player)
                {
                    // 通过 Authority 判断是否为发射者
                    int playerId = player.GetMultiplayerAuthority();
                    if (playerId == SourcePlayerId)
                    {
                        int selfDamage = Mathf.RoundToInt(_damage * 0.5f);
                        player.TakeDamage(selfDamage);
                    }
                }
            }
        }
    }

    public override void _Process(double delta)
    {
        _lifetime -= (float)delta;
        if (_lifetime <= 0) QueueFree();
    }
}
