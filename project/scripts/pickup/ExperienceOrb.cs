using Godot;

namespace Miao.Pickup;

/// <summary>
/// 经验球
/// 被玩家靠近后自动吸附并增加经验值
/// </summary>
public partial class ExperienceOrb : Area2D
{
    /// <summary>经验值</summary>
    [Export] public int Experience = 5;

    /// <summary>吸附速度</summary>
    [Export] public float MagnetSpeed = 300f;

    /// <summary>吸附范围</summary>
    [Export] public float MagnetRange = 80f;

    private Node2D _player;
    private bool _isMagnetized;

    public override void _Ready()
    {
        _player = GetTree().GetFirstNodeInGroup("player") as Node2D;
    }

    public override void _Process(double delta)
    {
        if (_player == null) return;

        float distance = GlobalPosition.DistanceTo(_player.GlobalPosition);

        // 进入吸附范围后开始飞向玩家
        if (distance < MagnetRange)
        {
            _isMagnetized = true;
        }

        if (_isMagnetized)
        {
            var direction = (_player.GlobalPosition - GlobalPosition).Normalized();
            GlobalPosition += direction * MagnetSpeed * (float)delta;

            // 到达玩家位置时拾取
            if (distance < 10f)
            {
                Collect();
            }
        }
    }

    private void Collect()
    {
        // 通知玩家获得经验
        if (_player is Player.Player player)
        {
            player.AddExperience(Experience);
        }
        QueueFree();
    }
}
