using Godot;

namespace Miao.Enemy;

public enum EliteModType
{
    Speed,     // 加速
    Shield,    // 护盾
    Split,     // 分裂
    Regen      // 回血
}

public partial class EliteModifier : Node
{
    public EliteModType Type { get; private set; }
    private Enemy _enemy;
    private float _regenTimer;

    public void Init(Enemy enemy, EliteModType type)
    {
        _enemy = enemy;
        Type = type;
        Apply();
    }

    private void Apply()
    {
        switch (Type)
        {
            case EliteModType.Speed:
                _enemy.MoveSpeed *= 1.5f;
                break;
            case EliteModType.Shield:
                _enemy.AddShield(_enemy.MaxHealth * 0.5f);
                break;
            case EliteModType.Regen:
                break;
            case EliteModType.Split:
                break;
        }
    }

    public override void _Process(double delta)
    {
        if (Type == EliteModType.Regen)
        {
            _regenTimer -= (float)delta;
            if (_regenTimer <= 0)
            {
                _regenTimer = 1.0f;
                _enemy.Heal(Mathf.RoundToInt(_enemy.MaxHealth * 0.02f));
            }
        }
    }

    public void OnDeath()
    {
        if (Type == EliteModType.Split)
        {
            for (int i = 0; i < 2; i++)
            {
                var smallBug = GD.Load<PackedScene>("res://scenes/enemy/SmallBug.tscn").Instantiate<Enemy>();
                smallBug.GlobalPosition = _enemy.GlobalPosition + new Vector2(GD.RandRange(-30, 30), GD.RandRange(-30, 30));
                smallBug.MaxHealth = _enemy.MaxHealth / 3;
                smallBug.CurrentHealth = smallBug.MaxHealth;
                GetTree().CurrentScene.AddChild(smallBug);
            }
        }
    }
}
