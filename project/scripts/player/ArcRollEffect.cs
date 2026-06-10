using Godot;

namespace Miao.Player;

public partial class ArcRollEffect : Node
{
    private float _invincibleTimer = 0;

    public void Apply(Player player)
    {
        _invincibleTimer = 0.5f;
        // TODO: visual effect (blue trail)
    }

    public override void _Process(double delta)
    {
        if (_invincibleTimer > 0)
        {
            _invincibleTimer -= (float)delta;
        }
    }

    public bool IsInvincible()
    {
        return _invincibleTimer > 0;
    }
}
