using Godot;
using System.Collections.Generic;

namespace Miao.Enemy;

public partial class BugQueen : Enemy
{
    public enum BossPhase { Phase1, Phase2, Phase3 }

    [Export] public PackedScene SmallBugScene;

    private BossPhase _currentPhase = BossPhase.Phase1;
    private float _attackTimer;
    private float _summonTimer;
    private float _phaseTransitionTimer;
    private bool _isTransitioning;
    private List<Node2D> _poisonPools = new();

    public override void _Ready()
    {
        MaxHealth = 500;
        MoveSpeed = 60;
        ContactDamage = 25;
        base._Ready();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (_isTransitioning)
        {
            _phaseTransitionTimer -= (float)delta;
            if (_phaseTransitionTimer <= 0)
            {
                _isTransitioning = false;
                GD.Print($"虫后进入阶段 {_currentPhase + 1}");
            }
            return;
        }

        _attackTimer -= (float)delta;
        _summonTimer -= (float)delta;

        switch (_currentPhase)
        {
            case BossPhase.Phase1:
                UpdatePhase1(delta);
                break;
            case BossPhase.Phase2:
                UpdatePhase2(delta);
                break;
            case BossPhase.Phase3:
                UpdatePhase3(delta);
                break;
        }

        CheckPhaseTransition();
    }

    private void UpdatePhase1(double delta)
    {
        if (_summonTimer <= 0)
        {
            _summonTimer = 4.0f;
            SummonSmallBugs(3);
        }

        if (_attackTimer <= 0)
        {
            _attackTimer = 3.0f;
            SpawnPoisonPool();
        }
    }

    private void UpdatePhase2(double delta)
    {
        if (_attackTimer <= 0)
        {
            _attackTimer = 3.5f;
            ChargeAttack();
        }

        if (_summonTimer <= 0)
        {
            _summonTimer = 6.0f;
            SummonSmallBugs(2);
        }
    }

    private void UpdatePhase3(double delta)
    {
        if (_attackTimer <= 0)
        {
            _attackTimer = 2.0f;
            SpawnPoisonPool();
        }

        if (_summonTimer <= 0)
        {
            _summonTimer = 3.0f;
            SummonSmallBugs(5);
        }

        foreach (var pool in _poisonPools)
        {
            if (IsInstanceValid(pool))
            {
                pool.Scale = pool.Scale.Lerp(new Vector2(3, 3), 0.01f);
            }
        }
    }

    private void CheckPhaseTransition()
    {
        float healthPercent = (float)CurrentHealth / MaxHealth;

        if (_currentPhase == BossPhase.Phase1 && healthPercent <= 0.6f)
        {
            TransitionToPhase(BossPhase.Phase2);
        }
        else if (_currentPhase == BossPhase.Phase2 && healthPercent <= 0.3f)
        {
            TransitionToPhase(BossPhase.Phase3);
        }
    }

    private void TransitionToPhase(BossPhase newPhase)
    {
        _currentPhase = newPhase;
        _isTransitioning = true;
        _phaseTransitionTimer = 2.0f;
        GD.Print("虫后硬直！输出窗口！");

        foreach (var pool in _poisonPools)
        {
            if (IsInstanceValid(pool)) pool.QueueFree();
        }
        _poisonPools.Clear();
    }

    private void SummonSmallBugs(int count)
    {
        if (SmallBugScene == null) return;

        for (int i = 0; i < count; i++)
        {
            var bug = SmallBugScene.Instantiate<Enemy>();
            bug.GlobalPosition = GlobalPosition + new Vector2(GD.RandRange(-50, 50), GD.RandRange(-50, 50));
            GetTree().CurrentScene.AddChild(bug);
        }
    }

    private void SpawnPoisonPool()
    {
        var players = GetTree().GetNodesInGroup("player");
        if (players.Count == 0) return;

        var pool = new Area2D();
        pool.CollisionLayer = 2;
        pool.CollisionMask = 1;

        var shape = new CircleShape2D();
        shape.Radius = 40;
        var collision = new CollisionShape2D();
        collision.Shape = shape;
        pool.AddChild(collision);

        var colorRect = new ColorRect();
        colorRect.Size = new Vector2(80, 80);
        colorRect.Position = new Vector2(-40, -40);
        colorRect.Color = new Color(0, 0.5f, 0, 0.4f);
        pool.AddChild(colorRect);

        pool.GlobalPosition = ((Node2D)players[0]).GlobalPosition;

        pool.BodyEntered += (body) =>
        {
            if (body is Miao.Player.Player player)
            {
                player.TakeDamage(5);
            }
        };

        GetTree().CurrentScene.AddChild(pool);
        _poisonPools.Add(pool);

        GetTree().CreateTimer(8.0).Timeout += () =>
        {
            if (IsInstanceValid(pool)) pool.QueueFree();
            _poisonPools.Remove(pool);
        };
    }

    private void ChargeAttack()
    {
        var players = GetTree().GetNodesInGroup("player");
        if (players.Count == 0) return;

        var target = ((Node2D)players[0]).GlobalPosition;
        var direction = (target - GlobalPosition).Normalized();

        var tween = CreateTween();
        tween.TweenProperty(this, "global_position", GlobalPosition + direction * 300, 0.5f);
    }
}
