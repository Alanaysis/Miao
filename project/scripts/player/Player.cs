using Godot;
using Miao.System;
using Miao.Weapon;

namespace Miao.Player;

/// <summary>
/// 玩家角色控制脚本
/// 负责移动、生命值管理、经验升级
/// </summary>
public partial class Player : CharacterBody2D
{
    /// <summary>移动速度</summary>
    [Export] public float MoveSpeed = 200f;

    /// <summary>子弹场景</summary>
    [Export] public PackedScene BulletScene;

    private float _aimAngle;
    private Sprite2D _sprite;
    private Node2D _weaponSlot;

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

    /// <summary>射击信号</summary>
    [Signal]
    public delegate void ShootEventHandler();

    /// <summary>武器拾取选择信号</summary>
    [Signal]
    public delegate void WeaponPickupChoiceEventHandler(WeaponData newWeapon);

    private WeaponData _nearbyWeaponData;
    private Node2D _nearbyWeaponDrop;

    public EquipmentSlot Equipment { get; private set; }
    public SubclassManager Subclass { get; private set; }
    public FragmentManager Fragments { get; private set; }

    // 技能系统
    [Export] public float Skill1Cooldown = 8.0f;
    [Export] public float Skill2Cooldown = 15.0f;
    [Export] public float SuperChargePerKill = 5.0f;
    [Export] public float SuperMaxCharge = 100.0f;

    public float Skill1Timer { get; protected set; }
    public float Skill2Timer { get; protected set; }
    public float SuperCharge { get; protected set; }
    public bool IsSuperReady => SuperCharge >= SuperMaxCharge;

    // 护盾
    public float Shield { get; protected set; }
    public float MaxShield { get; protected set; }

    [Signal]
    public delegate void Skill1UsedEventHandler();
    [Signal]
    public delegate void Skill2UsedEventHandler();
    [Signal]
    public delegate void SuperUsedEventHandler();
    [Signal]
    public delegate void ShieldChangedEventHandler(float current, float max);
    [Signal]
    public delegate void SuperChargeChangedEventHandler(float current, float max);

    public override void _Ready()
    {
        // 应用 MetaProgression 加成
        if (MetaProgression.Instance != null)
        {
            MaxHealth += MetaProgression.Instance.GetBonusHealth();
            MoveSpeed *= MetaProgression.Instance.GetBonusMoveSpeed();
        }

        CurrentHealth = MaxHealth;
        AddToGroup("player");
        _sprite = GetNode<Sprite2D>("Sprite2D");
        _weaponSlot = GetNode<Node2D>("WeaponSlot");
        Equipment = GetNode<EquipmentSlot>("WeaponSlot/EquipmentSlot");

        // 初始化子职业管理器
        Subclass = new SubclassManager();
        AddChild(Subclass);

        // 初始化碎片管理器
        Fragments = new FragmentManager();
        AddChild(Fragments);

        // 加载当前子职业的碎片池
        LoadFragmentsForCurrentSubclass();

        // 注册到GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterPlayer(this);
        }
    }

    public override void _Process(double delta)
    {
        if (Skill1Timer > 0) Skill1Timer -= (float)delta;
        if (Skill2Timer > 0) Skill2Timer -= (float)delta;
    }

    public override void _PhysicsProcess(double delta)
    {
        // 移动（高位俯视角，8方向移动）
        var inputDir = Input.GetVector("move_left", "move_right", "move_up", "move_down");
        Velocity = inputDir * MoveSpeed;
        MoveAndSlide();

        // 瞄准：武器独立朝向鼠标（角色本身不旋转）
        var mousePos = GetGlobalMousePosition();
        _aimAngle = (mousePos - GlobalPosition).Angle();
        _weaponSlot.Rotation = _aimAngle;

        // 精灵翻转：朝向鼠标
        _sprite.FlipH = mousePos.X < GlobalPosition.X;

        // 按住左键持续射击
        if (Input.IsActionPressed("shoot"))
        {
            EmitSignal(SignalName.Shoot);
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("skill_1") && Skill1Timer <= 0)
        {
            UseSkill1();
        }
        if (@event.IsActionPressed("skill_2") && Skill2Timer <= 0)
        {
            UseSkill2();
        }
        if (@event.IsActionPressed("super_ability") && IsSuperReady)
        {
            UseSuper();
        }
        if (@event.IsActionPressed("interact"))
        {
            GameManager.Instance?.TryInteract();
        }
    }

    /// <summary>
    /// 对玩家造成伤害
    /// </summary>
    /// <param name="damage">伤害值</param>
    public void TakeDamage(int damage)
    {
        // 先扣护盾
        float shieldAbsorb = Mathf.Min(Shield, damage);
        Shield -= shieldAbsorb;
        damage -= Mathf.RoundToInt(shieldAbsorb);

        CurrentHealth -= damage;
        CurrentHealth = Mathf.Max(CurrentHealth, 0);
        EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);
        EmitSignal(SignalName.ShieldChanged, Shield, MaxShield);

        if (CurrentHealth <= 0) Die();
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

    protected virtual void UseSkill1() { }
    protected virtual void UseSkill2() { }
    protected virtual void UseSuper() { }

    public void AddSuperCharge(float amount)
    {
        SuperCharge = Mathf.Min(SuperCharge + amount, SuperMaxCharge);
        EmitSignal(SignalName.SuperChargeChanged, SuperCharge, SuperMaxCharge);
    }

    private void Die()
    {
        EmitSignal(SignalName.PlayerDied);
        GD.Print("玩家死亡！");
    }

    public void SetNearbyWeapon(WeaponData data, Node2D drop)
    {
        _nearbyWeaponData = data;
        _nearbyWeaponDrop = drop;
    }

    public void ClearNearbyWeapon()
    {
        _nearbyWeaponData = null;
        _nearbyWeaponDrop = null;
    }

    private void HandleWeaponPickup()
    {
        if (Equipment.CurrentWeapon == null)
        {
            Equipment.EquipWeapon(_nearbyWeaponData);
            _nearbyWeaponDrop?.QueueFree();
            _nearbyWeaponData = null;
            _nearbyWeaponDrop = null;
        }
        else
        {
            EmitSignal(SignalName.WeaponPickupChoice, _nearbyWeaponData);
        }
    }

    /// <summary>
    /// 为当前子职业加载碎片池
    /// </summary>
    private void LoadFragmentsForCurrentSubclass()
    {
        if (Subclass == null || Fragments == null) return;

        string className = "hunter"; // 默认职业
        string subclassKey = Subclass.ActiveSubclass.ToString().ToLower();
        Fragments.LoadFragmentsForSubclass(className, subclassKey);
    }
}
