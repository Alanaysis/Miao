using Godot;

namespace Miao.Network;

public partial class NetworkSync : MultiplayerSynchronizer
{
    [Export] public float SyncInterval = 0.05f; // 20Hz sync

    private float _syncTimer;
    private Node2D _parent;

    public override void _Ready()
    {
        _parent = GetParent<Node2D>();
        // Only the authority (owner) should sync
        SetMultiplayerAuthority(int.Parse(Name));
    }

    public override void _Process(double delta)
    {
        if (!IsMultiplayerAuthority()) return;

        _syncTimer -= (float)delta;
        if (_syncTimer <= 0)
        {
            _syncTimer = SyncInterval;
            SyncState();
        }
    }

    private void SyncState()
    {
        if (_parent == null) return;
        // Sync position, health, weapon state
        Rpc(nameof(ReceiveState), _parent.GlobalPosition);
    }

    [Rpc]
    private void ReceiveState(Vector2 position)
    {
        if (IsMultiplayerAuthority()) return;
        if (_parent == null) return;
        // Apply interpolated state
        _parent.GlobalPosition = _parent.GlobalPosition.Lerp(position, 0.3f);
    }
}
