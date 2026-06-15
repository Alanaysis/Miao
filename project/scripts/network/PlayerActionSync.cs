using Godot;

namespace Miao.Network;

/// <summary>
/// Broadcasts player actions (position, shoot, skill) to other players.
/// Each player instance has this as a child; the local player broadcasts,
/// remote players receive.
/// </summary>
public partial class PlayerActionSync : Node
{
    private float _positionTimer;
    private const float PositionInterval = 0.05f; // 20 Hz

    public override void _Process(double delta)
    {
        if (!Multiplayer.HasMultiplayerPeer()) return;

        var parent = GetParent<Node2D>();

        // Only the local player (authority) broadcasts position
        if (!IsLocalPlayer()) return;

        _positionTimer -= (float)delta;
        if (_positionTimer <= 0)
        {
            _positionTimer = PositionInterval;
            Rpc(nameof(ReceivePosition), parent.GlobalPosition, parent.Rotation);
        }
    }

    private bool IsLocalPlayer()
    {
        var parent = GetParent();
        int parentId = parent.Name.ToString().ToInt();
        return parentId == Multiplayer.GetUniqueId();
    }

    /// <summary>
    /// Receive interpolated position from a remote player.
    /// </summary>
    [Rpc]
    public void ReceivePosition(Vector2 position, float rotation)
    {
        if (IsLocalPlayer()) return;

        var parent = GetParent<Node2D>();
        parent.GlobalPosition = parent.GlobalPosition.Lerp(position, 0.3f);
        parent.Rotation = rotation;
    }

    /// <summary>
    /// Receive shoot broadcast on remote clients — spawn visual bullet.
    /// </summary>
    [Rpc]
    public void ReceiveShoot(Vector2 position, Vector2 direction, int weaponType)
    {
        if (IsLocalPlayer()) return;

        // 在远程玩家位置生成视觉子弹（仅视觉，无伤害）
        SpawnVisualBullet(position, direction, weaponType);
    }

    /// <summary>
    /// Receive skill broadcast on remote clients — spawn skill effect.
    /// </summary>
    [Rpc]
    public void ReceiveSkill(int skillIndex, Vector2 position, Vector2 direction)
    {
        if (IsLocalPlayer()) return;

        // 生成技能视觉特效
        SpawnSkillEffect(skillIndex, position, direction);
    }

    /// <summary>
    /// Receive damage taken broadcast — show floating damage number.
    /// </summary>
    [Rpc]
    public void ReceiveDamageTaken(int damage)
    {
        if (IsLocalPlayer()) return;

        // 在远程玩家头上显示伤害数字
        var parent = GetParent<Node2D>();
        SpawnFloatingDamageNumber(parent.GlobalPosition, damage);
    }

    /// <summary>
    /// 生成视觉子弹（仅渲染，无碰撞伤害）
    /// </summary>
    private void SpawnVisualBullet(Vector2 position, Vector2 direction, int weaponType)
    {
        var bullet = new Node2D();
        bullet.GlobalPosition = position;
        bullet.Rotation = direction.Angle();

        // 子弹视觉
        var visual = new ColorRect();
        visual.Size = new Vector2(12, 4);
        visual.Position = new Vector2(-6, -2);
        visual.Color = new Color(1.0f, 0.9f, 0.3f);
        bullet.AddChild(visual);

        GetTree().CurrentScene.AddChild(bullet);

        // 子弹飞行动画
        var tween = GetTree().CreateTween();
        tween.TweenProperty(bullet, "position", position + direction * 400, 0.3);
        tween.TweenCallback(Callable.From(() =>
        {
            if (IsInstanceValid(bullet)) bullet.QueueFree();
        }));
    }

    /// <summary>
    /// 生成技能视觉特效
    /// </summary>
    private void SpawnSkillEffect(int skillIndex, Vector2 position, Vector2 direction)
    {
        // 简单的技能特效：圆形扩散
        var effect = new Node2D();
        effect.GlobalPosition = position;

        var circle = new ColorRect();
        circle.Size = new Vector2(20, 20);
        circle.Position = new Vector2(-10, -10);
        circle.Color = new Color(0.3f, 0.6f, 1.0f, 0.8f);
        effect.AddChild(circle);

        GetTree().CurrentScene.AddChild(effect);

        // 扩散 + 消失动画
        var tween = GetTree().CreateTween();
        tween.TweenProperty(effect, "scale", new Vector2(3, 3), 0.3);
        tween.Parallel().TweenProperty(circle, "modulate:a", 0.0f, 0.3);
        tween.TweenCallback(Callable.From(() =>
        {
            if (IsInstanceValid(effect)) effect.QueueFree();
        }));
    }

    /// <summary>
    /// 生成浮动伤害数字
    /// </summary>
    private void SpawnFloatingDamageNumber(Vector2 position, int damage)
    {
        var label = new Label();
        label.Text = damage.ToString();
        label.GlobalPosition = position + new Vector2(-10, -30);
        label.AddThemeColorOverride("font_color", new Color(1, 0.3f, 0.3f));
        label.AddThemeFontSizeOverride("font_size", 16);
        label.ZIndex = 20;
        GetTree().CurrentScene.AddChild(label);

        // 上浮 + 消失动画
        var tween = GetTree().CreateTween();
        tween.TweenProperty(label, "position", label.Position + new Vector2(0, -40), 0.8);
        tween.Parallel().TweenProperty(label, "modulate:a", 0.0f, 0.8);
        tween.TweenCallback(Callable.From(() =>
        {
            if (IsInstanceValid(label)) label.QueueFree();
        }));
    }
}
