using Godot;

namespace Miao.Player;

/// <summary>
/// 侦察兵角色
/// 低生命值，高速度，远程专精
/// </summary>
public partial class Scout : Player
{
    public override void _Ready()
    {
        MoveSpeed = 260f;
        MaxHealth = 70;
        base._Ready();
    }
}
