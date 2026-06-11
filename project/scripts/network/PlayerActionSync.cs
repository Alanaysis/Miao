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
    /// Broadcast a shoot event. Called by the local player when firing.
    /// </summary>
    [Rpc]
    public void BroadcastShoot(Vector2 position, Vector2 direction, int weaponType)
    {
        if (IsLocalPlayer()) return;

        // Show shoot visual on remote player
        // TODO: spawn visual bullet for remote player
    }

    /// <summary>
    /// Receive shoot broadcast on remote clients.
    /// </summary>
    [Rpc]
    public void ReceiveShoot(Vector2 position, Vector2 direction, int weaponType)
    {
        if (IsLocalPlayer()) return;

        // TODO: spawn visual bullet effect
    }

    /// <summary>
    /// Broadcast a skill event. Called by the local player when using a skill.
    /// </summary>
    [Rpc]
    public void BroadcastSkill(int skillIndex, Vector2 position, Vector2 direction)
    {
        if (IsLocalPlayer()) return;

        // Show skill visual on remote player
        // TODO: spawn skill effect
    }

    /// <summary>
    /// Receive skill broadcast on remote clients.
    /// </summary>
    [Rpc]
    public void ReceiveSkill(int skillIndex, Vector2 position, Vector2 direction)
    {
        if (IsLocalPlayer()) return;

        // TODO: spawn skill effect visual
    }

    /// <summary>
    /// Broadcast damage taken to show numbers on remote clients.
    /// </summary>
    [Rpc]
    public void BroadcastDamageTaken(int damage)
    {
        if (IsLocalPlayer()) return;

        // Show damage number on remote player
        // TODO: spawn floating damage number
    }

    /// <summary>
    /// Receive damage taken broadcast.
    /// </summary>
    [Rpc]
    public void ReceiveDamageTaken(int damage)
    {
        if (IsLocalPlayer()) return;

        // TODO: show floating damage number visual
    }
}
