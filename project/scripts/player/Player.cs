using Godot;
using Miao.System;

namespace Miao.Player;

/// <summary>
/// 玩家角色控制脚本
/// 负责移动、生命值管理、经验升级
/// </summary>
public partial class Player : CharacterBody2D
{
    /// <summary>移动速度</summary>
    [Export] public float MoveSpeed = 200f;

    /// <summary>最大生命值</summary>
    [Export] public int MaxHealth = 100;

    /// <summary>当前生命值</summary>
    public int CurrentHealth { get; private set; }

    /// <summary>当前等级</summary>
    public int Level { get; private set; } = 1;

    /// <summary>当前经验值</summary>
    public int CurrentExperience { get; private set; }

    /// <summary>当前等级所需经验</summary>
    public int ExperienceToLevel { get; private set; } = 10;

    /// <summary>击杀数</summary>
    public int KillCount { get; private set; }

    /// <summary>生命值变化信号</summary>
    [Signal]
    public delegate void HealthChangedEventHandler(int currentHealth, int maxHealth);

    /// <summary>玩家死亡信号</summary>
    [Signal]
    public delegate void PlayerDiedEventHandler();

    /// <summary>经验变化信号</summary>
    [Signal]
    public delegate void ExperienceChangedEventHandler(int currentExp, int expToLevel, int level);

    /// <summary>升级信号</summary>
    [Signal]
    public delegate void LevelUpEventHandler(int newLevel);

    public override void _Ready()
    {
        CurrentHealth = MaxHealth;
        AddToGroup("player");

        // 注册到GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterPlayer(this);
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        var input = Input.GetVector("move_left", "move_right", "move_up", "move_down");
        Velocity = input * MoveSpeed;
        MoveAndSlide();
    }

    /// <summary>
    /// 对玩家造成伤害
    /// </summary>
    /// <param name="damage">伤害值</param>
    public void TakeDamage(int damage)
    {
        CurrentHealth = Mathf.Clamp(CurrentHealth - damage, 0, MaxHealth);
        EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);

        if (CurrentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// 恢复生命值
    /// </summary>
    /// <param name="amount">恢复量</param>
    public void Heal(int amount)
    {
        CurrentHealth = Mathf.Clamp(CurrentHealth + amount, 0, MaxHealth);
        EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);
    }

    /// <summary>
    /// 增加经验值
    /// </summary>
    /// <param name="amount">经验量</param>
    public void AddExperience(int amount)
    {
        CurrentExperience += amount;
        EmitSignal(SignalName.ExperienceChanged, CurrentExperience, ExperienceToLevel, Level);

        // 检查升级
        while (CurrentExperience >= ExperienceToLevel)
        {
            CurrentExperience -= ExperienceToLevel;
            PerformLevelUp();
        }
    }

    /// <summary>
    /// 增加击杀数
    /// </summary>
    public void AddKill()
    {
        KillCount++;
    }

    /// <summary>
    /// 升级处理
    /// </summary>
    private void PerformLevelUp()
    {
        Level++;
        // 经验需求递增：每级增加 50%
        ExperienceToLevel = (int)(10 * Mathf.Pow(1.5, Level - 1));
        EmitSignal(SignalName.LevelUp, Level);
        EmitSignal(SignalName.ExperienceChanged, CurrentExperience, ExperienceToLevel, Level);
        GD.Print($"升级！当前等级: {Level}");
    }

    private void Die()
    {
        EmitSignal(SignalName.PlayerDied);
        GD.Print("玩家死亡！");
    }
}
