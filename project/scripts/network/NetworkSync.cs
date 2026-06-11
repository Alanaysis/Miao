using Godot;

namespace Miao.Network;

public partial class NetworkSync : MultiplayerSynchronizer
{
    [Export] public float SyncInterval = 0.05f; // 20Hz sync

    private float _syncTimer;

    public override void _Ready()
    {
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
        // Sync position, health, weapon state
        Rpc(nameof(ReceiveState), GlobalPosition);
    }

    [Rpc(MultiplayerApi.TransferMode.Unreliable)]
    private void ReceiveState(Vector2 position)
    {
        if (IsMultiplayerAuthority()) return;
        // Apply interpolated state
        GlobalPosition = GlobalPosition.Lerp(position, 0.3f);
    }
}
