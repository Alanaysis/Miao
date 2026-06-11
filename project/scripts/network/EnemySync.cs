using Godot;
using EnemyClass = Miao.Enemy.Enemy;

namespace Miao.Network;

/// <summary>
/// Enemy state synchronization — host controls enemy AI, clients receive state.
/// Attached to each enemy scene as a child node.
/// </summary>
public partial class EnemySync : Node
{
    private EnemyClass _enemy;
    private int _networkId;
    private float _syncTimer;
    private const float SyncInterval = 0.1f; // 10 Hz for enemies

    public override void _Ready()
    {
        _enemy = GetParent<EnemyClass>();
    }

    public void SetNetworkId(int id)
    {
        _networkId = id;
    }

    public override void _Process(double delta)
    {
        if (!Multiplayer.HasMultiplayerPeer()) return;
        if (!NetworkManager.Instance.IsHost) return;

        _syncTimer -= (float)delta;
        if (_syncTimer <= 0)
        {
            _syncTimer = SyncInterval;
            Rpc(nameof(ReceiveEnemyState), _networkId,
                _enemy.GlobalPosition, _enemy.CurrentHealth, false);
        }
    }

    /// <summary>
    /// Receive enemy state from host. Called on clients only.
    /// </summary>
    [Rpc]
    public void ReceiveEnemyState(int enemyId, Vector2 position, int health, bool isDead)
    {
        if (NetworkManager.Instance.IsHost) return;

        // Apply enemy state from host with interpolation
        _enemy.GlobalPosition = _enemy.GlobalPosition.Lerp(position, 0.3f);
        _enemy.SetNetworkHealth(health);

        if (isDead && !_enemy.IsQueuedForDeletion())
        {
            _enemy.QueueFree();
        }
    }

    /// <summary>
    /// Notify clients that this enemy has died. Host calls this before freeing.
    /// </summary>
    [Rpc]
    public void NotifyEnemyDeath(int enemyId)
    {
        if (NetworkManager.Instance.IsHost) return;

        if (!_enemy.IsQueuedForDeletion())
        {
            _enemy.QueueFree();
        }
    }

    /// <summary>
    /// Client sends damage request to host. Host validates and applies.
    /// </summary>
    [Rpc]
    public void RequestDamage(int enemyId, int damage, int sourcePlayerId)
    {
        if (!NetworkManager.Instance.IsHost) return;

        // Host validates and applies damage
        _enemy.TakeDamage(damage);
    }
}
