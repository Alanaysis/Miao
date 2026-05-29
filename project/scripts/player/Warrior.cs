using Godot;

namespace Miao.Player;

/// <summary>
/// 战士角色
/// 高生命值，中等速度，近战专精
/// </summary>
public partial class Warrior : Player
{
    public override void _Ready()
    {
        MoveSpeed = 180f;
        MaxHealth = 120;
        base._Ready();
    }
}
