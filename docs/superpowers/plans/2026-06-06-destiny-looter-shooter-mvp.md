# Destiny-Inspired Looter Shooter MVP — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a playable looter-shooter MVP with free-aim shooting, weapon perks, hunter class, room-based progression, and a boss fight.

**Architecture:** Extend the existing roguelike C# codebase. Transform auto-attack weapons into mouse-aimed shooting, replace infinite spawning with room-based waves, add perk/loot/equipment systems. Keep the CharacterBody2D-based player/enemy architecture.

**Tech Stack:** Godot 4.x, C#, .NET

---

## File Structure

### Files to Modify (from roguelike branch)

| File | Changes |
|------|---------|
| `project/scripts/player/Player.cs` | Add mouse aiming, skill slots, equipment slot, shield |
| `project/scripts/player/Warrior.cs` | Remove (replaced by Hunter) |
| `project/scripts/player/Scout.cs` | Remove (replaced by Hunter) |
| `project/scripts/weapon/Weapon.cs` | Add rarity, perk slots, weapon type enum |
| `project/scripts/weapon/Bullet.cs` | Add penetration, owner tracking |
| `project/scripts/weapon/Projectile.cs` | Aim at mouse direction, support shotgun spread |
| `project/scripts/enemy/Enemy.cs` | Add elite modifier system, loot drop on death |
| `project/scripts/system/GameManager.cs` | Room-based flow: rooms → boss → settlement |
| `project/scripts/system/SpawnManager.cs` | Wave-based spawning per room instead of infinite |
| `project/scripts/system/MetaProgression.cs` | Glimmer currency, permanent upgrades, codex |
| `project/scripts/ui/HUD.cs` | Redesign: health bar, shield, ability CDs, weapon info, super bar |
| `project/scripts/ui/MainMenu.cs` | Add class selection, lobby layout |
| `project/scripts/ui/GameOverUI.cs` | Replace with settlement screen (weapon keep/decompose) |
| `project/scripts/ui/LevelUpUI.cs` | Remove (replaced by loot pickup) |
| `project/scenes/player/Player.tscn` | Remove SpinBlade, add weapon slot node |
| `project/scenes/main.tscn` | Restructure for room-based layout |
| `project/project.godot` | Add `shoot`, `skill_1`, `skill_2`, `super_ability`, `interact` inputs |

### Files to Create

| File | Responsibility |
|------|---------------|
| `project/scripts/player/Hunter.cs` | Hunter class: stats, skills, super ability |
| `project/scripts/weapon/WeaponData.cs` | Weapon data class: type, rarity, perks, stats |
| `project/scripts/weapon/PerkSystem.cs` | Perk pool, random generation, effect application |
| `project/scripts/weapon/LootTable.cs` | Drop probability by room/layer |
| `project/scripts/weapon/EquipmentSlot.cs` | Single weapon slot, equip/absorb logic |
| `project/scripts/enemy/EliteModifier.cs` | Elite enemy modifiers (speed/shield/split/regen) |
| `project/scripts/enemy/BugQueen.cs` | Boss AI: 3-phase state machine |
| `project/scripts/system/RoomGenerator.cs` | Random room layout + wave composition |
| `project/scripts/system/SettlementManager.cs` | Post-run settlement: keep/decompose weapons |
| `project/scripts/ui/PickupPrompt.cs` | "E to equip / E to absorb" UI prompt |
| `project/scripts/ui/WeaponInfoUI.cs` | Weapon name, rarity, perk display |
| `project/scripts/ui/SkillCooldownUI.cs` | Q/R/F skill icons with CD overlay |

---

## Task 1: Project Setup — Merge Roguelike Branch

**Goal:** Get the existing roguelike code running on the current branch as a starting point.

- [ ] **Step 1: Cherry-pick roguelike scripts into current branch**

```bash
cd /home/siok/Miao
git checkout experiment/roguelike -- project/scripts/ project/scenes/ project/project.godot
```

- [ ] **Step 2: Verify the file structure**

```bash
ls project/scripts/player/ project/scripts/weapon/ project/scripts/enemy/ project/scripts/system/ project/scripts/ui/
```

Expected: All C# files from the roguelike branch are present.

- [ ] **Step 3: Remove old Warrior/Scout classes (will be replaced by Hunter)**

```bash
rm project/scripts/player/Warrior.cs
rm project/scripts/player/Scout.cs
rm project/scenes/player/Warrior.tscn
rm project/scenes/player/Scout.tscn
```

- [ ] **Step 4: Update project.godot input mappings**

Add these input actions to `project/project.godot`:

```
[input]
shoot={
"deadzone": 0.5,
"events": [Object(InputEventMouseButton,"resource_local_to_scene":false,"resource_name":"","device":-1,"window_id":0,"alt_pressed":false,"shift_pressed":false,"ctrl_pressed":false,"meta_pressed":false,"button_mask":1,"position":Vector2(0, 0),"global_position":Vector2(0, 0),"factor":1.0,"button_index":1,"canceled":false,"pressed":true,"double_click":false)]
}
skill_1={
"deadzone": 0.5,
"events": [Object(InputEventKey,"resource_local_to_scene":false,"resource_name":"","device":-1,"window_id":0,"alt_pressed":false,"shift_pressed":false,"ctrl_pressed":false,"meta_pressed":false,"pressed":false,"keycode":81,"physical_keycode":0,"key_label":0,"unicode":113,"location":0,"echo":false)]
}
skill_2={
"deadzone": 0.5,
"events": [Object(InputEventKey,"resource_local_to_scene":false,"resource_name":"","device":-1,"window_id":0,"alt_pressed":false,"shift_pressed":false,"ctrl_pressed":false,"meta_pressed":false,"pressed":false,"keycode":82,"physical_keycode":0,"key_label":0,"unicode":114,"location":0,"echo":false)]
}
super_ability={
"deadzone": 0.5,
"events": [Object(InputEventKey,"resource_local_to_scene":false,"resource_name":"","device":-1,"window_id":0,"alt_pressed":false,"shift_pressed":false,"ctrl_pressed":false,"meta_pressed":false,"pressed":false,"keycode":70,"physical_keycode":0,"key_label":0,"unicode":102,"location":0,"echo":false)]
}
interact={
"deadzone": 0.5,
"events": [Object(InputEventKey,"resource_local_to_scene":false,"resource_name":"","device":-1,"window_id":0,"alt_pressed":false,"shift_pressed":false,"ctrl_pressed":false,"meta_pressed":false,"pressed":false,"keycode":69,"physical_keycode":0,"key_label":0,"unicode":101,"location":0,"echo":false)]
}
```

- [ ] **Step 5: Commit**

```bash
git add project/scripts/ project/scenes/ project/project.godot
git commit -m "chore: merge roguelike codebase as starting point"
```

---

## Task 2: Player Aiming — Free Aim with Mouse

**Goal:** Replace auto-attack with mouse-aimed shooting. Player rotates to face mouse cursor, fires bullets on click.

### Files:
- Modify: `project/scripts/player/Player.cs`
- Modify: `project/scenes/player/Player.tscn`

- [ ] **Step 1: Add aiming and shooting to Player.cs**

Replace the `_PhysicsProcess` method and add shooting logic:

```csharp
// In Player.cs class body, add:
[Export] public PackedScene BulletScene;

private float _aimAngle;

public override void _PhysicsProcess(double delta)
{
    // 移动
    var inputDir = Input.GetVector("move_left", "move_right", "move_up", "move_down");
    Velocity = inputDir * MoveSpeed;
    MoveAndSlide();

    // 瞄准：计算角色到鼠标的角度
    var mousePos = GetGlobalMousePosition();
    _aimAngle = (mousePos - GlobalPosition).Angle();
    Rotation = _aimAngle;
}

public override void _UnhandledInput(InputEvent @event)
{
    if (@event.IsActionPressed("shoot"))
    {
        EmitSignal(SignalName.Shoot);
    }
}
```

Add new signal:

```csharp
[Signal]
public delegate void ShootEventHandler();
```

- [ ] **Step 2: Update Player.tscn**

Remove the `SpinBlade` child node. Add a `Marker2D` child named `WeaponSlot` at the player's front (offset by ~20px in the facing direction).

- [ ] **Step 3: Test manually**

Run the game. Player should rotate to face mouse. Click should emit the Shoot signal (verify with GD.Print).

- [ ] **Step 4: Commit**

```bash
git add project/scripts/player/Player.cs project/scenes/player/Player.tscn
git commit -m "feat(player): add mouse aiming and shoot signal"
```

---

## Task 3: Weapon System Overhaul — Rarity + Perks + Shooting

**Goal:** Transform weapons from auto-attack to mouse-aimed shooting. Add rarity system and perk slots.

### Files:
- Modify: `project/scripts/weapon/Weapon.cs`
- Create: `project/scripts/weapon/WeaponData.cs`
- Modify: `project/scripts/weapon/Projectile.cs`
- Modify: `project/scripts/weapon/Bullet.cs`
- Delete: `project/scripts/weapon/SpinBlade.cs`

- [ ] **Step 1: Create WeaponData.cs — weapon data model**

```csharp
using Godot;
using System.Collections.Generic;

namespace Miao.Weapon;

public enum WeaponType
{
    AutoRifle,   // 自动步枪
    Shotgun,     // 霰弹枪
    HandCannon   // 手炮
}

public enum Rarity
{
    Common,      // 白
    Uncommon,    // 绿
    Rare,        // 蓝
    Epic,        // 紫
    Legendary    // 金
}

public partial class WeaponData : Resource
{
    [Export] public WeaponType Type { get; set; }
    [Export] public Rarity Rarity { get; set; }
    [Export] public string DisplayName { get; set; }
    [Export] public int BaseDamage { get; set; }
    [Export] public float FireRate { get; set; }          // 每秒射击次数
    [Export] public float BulletSpeed { get; set; }
    [Export] public int BulletCount { get; set; } = 1;    // 霰弹枪 > 1
    [Export] public float SpreadAngle { get; set; }        // 散射角度（弧度）
    [Export] public float KnockbackForce { get; set; }

    public List<PerkId> Perks { get; set; } = new();

    /// <summary>
    /// 稀有度对应的 Perk 槽数
    /// </summary>
    public int MaxPerkSlots => Rarity switch
    {
        Rarity.Common => 0,
        Rarity.Uncommon => 1,
        Rarity.Rare => 2,
        Rarity.Epic => 2,
        Rarity.Legendary => 2,
        _ => 0
    };
}
```

- [ ] **Step 2: Create Weapon.cs overhaul**

Replace the entire `Weapon.cs`:

```csharp
using Godot;

namespace Miao.Weapon;

public partial class Weapon : Node2D
{
    [Export] public PackedScene BulletScene;

    public WeaponData Data { get; private set; }
    private float _cooldownTimer;

    // Perk 修饰后的实际属性
    public float EffectiveFireRate => Data.FireRate * PerkSystem.GetFireRateMultiplier(Data);
    public int EffectiveDamage => Mathf.RoundToInt(Data.BaseDamage * PerkSystem.GetDamageMultiplier(Data));
    public float EffectiveKnockback => Data.KnockbackForce * PerkSystem.GetKnockbackMultiplier(Data);

    public void SetWeaponData(WeaponData data)
    {
        Data = data;
        _cooldownTimer = 0;
    }

    public override void _Process(double delta)
    {
        if (Data == null) return;

        _cooldownTimer -= (float)delta;
    }

    /// <summary>
    /// 由 Player 的 Shoot 信号调用
    /// </summary>
    public void TryFire()
    {
        if (Data == null || _cooldownTimer > 0) return;

        _cooldownTimer = 1.0f / EffectiveFireRate;

        // 计算射击方向（武器的全局旋转）
        var fireDirection = GlobalTransform.X.Normalized();

        if (Data.BulletCount <= 1)
        {
            FireBullet(fireDirection);
        }
        else
        {
            // 霰弹枪：多发子弹扇形散射
            float totalSpread = Data.SpreadAngle;
            float step = totalSpread / (Data.BulletCount - 1);
            float startAngle = -totalSpread / 2;

            for (int i = 0; i < Data.BulletCount; i++)
            {
                float offset = startAngle + step * i;
                var dir = fireDirection.Rotated(offset);
                FireBullet(dir);
            }
        }
    }

    private void FireBullet(Vector2 direction)
    {
        if (BulletScene == null) return;

        var bullet = BulletScene.Instantiate<Bullet>();
        bullet.GlobalPosition = GlobalPosition;
        bullet.Velocity = direction * Data.BulletSpeed;
        bullet.Damage = EffectiveDamage;
        bullet.Knockback = EffectiveKnockback;
        bullet.CanPenetrate = PerkSystem.HasPerk(Data, PerkId.Penetration);
        GetTree().CurrentScene.AddChild(bullet);
    }
}
```

- [ ] **Step 3: Update Bullet.cs — add penetration**

```csharp
using Godot;
using System.Collections.Generic;
using Miao.Enemy;

namespace Miao.Weapon;

public partial class Bullet : Area2D
{
    public Vector2 Velocity { get; set; }
    public int Damage { get; set; }
    public float Knockback { get; set; }
    public bool CanPenetrate { get; set; }

    private float _lifetime = 3.0f;
    private HashSet<Node2D> _hitTargets = new();

    public override void _Ready()
    {
        CollisionLayer = 0;
        CollisionMask = 2; // 敌人层

        var shape = new CircleShape2D();
        shape.Radius = 5;
        var collision = new CollisionShape2D();
        collision.Shape = shape;
        AddChild(collision);

        BodyEntered += OnBodyEntered;
    }

    public override void _Process(double delta)
    {
        Position += Velocity * (float)delta;
        _lifetime -= (float)delta;
        if (_lifetime <= 0) QueueFree();
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is Enemy enemy && !_hitTargets.Contains(body))
        {
            _hitTargets.Add(body);
            enemy.TakeDamage(Damage);
            enemy.ApplyKnockback(Velocity.Normalized() * Knockback);

            if (!CanPenetrate)
            {
                QueueFree();
            }
        }
    }
}
```

- [ ] **Step 4: Update Projectile.cs — aim at mouse, use WeaponData**

```csharp
using Godot;

namespace Miao.Weapon;

public partial class Projectile : Weapon
{
    // Weapon.FireBullet 已处理所有逻辑，Projectile 现在只是一个场景容器
    // 未来可在这里加特殊弹道（追踪弹等）
}
```

- [ ] **Step 5: Delete SpinBlade.cs**

```bash
rm project/scripts/weapon/SpinBlade.cs
```

- [ ] **Step 6: Create bullet scene**

Create `project/scenes/weapon/Bullet.tscn`:

```
Bullet (Area2D) [Bullet.cs]
  collision_layer = 0
  collision_mask = 2
```

- [ ] **Step 7: Commit**

```bash
git add project/scripts/weapon/ project/scenes/weapon/
git commit -m "feat(weapon): overhaul weapon system with rarity, perks, and mouse-aimed shooting"
```

---

## Task 4: Perk System

**Goal:** Implement the 8 perks with their effects.

### Files:
- Create: `project/scripts/weapon/PerkSystem.cs`

- [ ] **Step 1: Create PerkSystem.cs**

```csharp
using Godot;
using System.Collections.Generic;

namespace Miao.Weapon;

public enum PerkId
{
    KillClip,        // 杀戮弹夹
    CriticalMaster,  // 暴击大师
    ShotgunSpread,   // 霰弹扩散
    Penetration,     // 穿透弹
    KillReturn,      // 击杀回弹
    RapidFire,       // 急速射击
    StableGrip,      // 稳定握把
    HeadHunter       // 猎头者
}

public static class PerkSystem
{
    // Perk 名称和描述（中文）
    public static readonly Dictionary<PerkId, (string Name, string Desc)> PerkInfo = new()
    {
        { PerkId.KillClip, ("杀戮弹夹", "击杀后下一弹夹伤害+30%") },
        { PerkId.CriticalMaster, ("暴击大师", "暴击率+15%") },
        { PerkId.ShotgunSpread, ("霰弹扩散", "散射角+20%，覆盖更广") },
        { PerkId.Penetration, ("穿透弹", "子弹穿透1个敌人") },
        { PerkId.KillReturn, ("击杀回弹", "击杀回复1发子弹") },
        { PerkId.RapidFire, ("急速射击", "射速+20%") },
        { PerkId.StableGrip, ("稳定握把", "后坐力-30%") },
        { PerkId.HeadHunter, ("猎头者", "对精英/Boss伤害+25%") }
    };

    // Perk 对武器类型的限制
    public static readonly Dictionary<PerkId, WeaponType[]> PerkWeaponRestriction = new()
    {
        { PerkId.KillClip, new[] { WeaponType.AutoRifle, WeaponType.Shotgun, WeaponType.HandCannon } },
        { PerkId.CriticalMaster, new[] { WeaponType.AutoRifle, WeaponType.HandCannon } },
        { PerkId.ShotgunSpread, new[] { WeaponType.Shotgun } },
        { PerkId.Penetration, new[] { WeaponType.AutoRifle, WeaponType.HandCannon } },
        { PerkId.KillReturn, new[] { WeaponType.AutoRifle, WeaponType.Shotgun, WeaponType.HandCannon } },
        { PerkId.RapidFire, new[] { WeaponType.AutoRifle, WeaponType.HandCannon } },
        { PerkId.StableGrip, new[] { WeaponType.AutoRifle, WeaponType.Shotgun, WeaponType.HandCannon } },
        { PerkId.HeadHunter, new[] { WeaponType.AutoRifle, WeaponType.Shotgun, WeaponType.HandCannon } }
    };

    public static bool HasPerk(WeaponData data, PerkId perk)
    {
        return data.Perks.Contains(perk);
    }

    public static float GetFireRateMultiplier(WeaponData data)
    {
        float mult = 1.0f;
        if (HasPerk(data, PerkId.RapidFire)) mult *= 1.2f;
        return mult;
    }

    public static float GetDamageMultiplier(WeaponData data)
    {
        float mult = 1.0f;
        // 杀戮弹夹的加成在击杀时触发，这里返回基础倍率
        return mult;
    }

    public static float GetKnockbackMultiplier(WeaponData data)
    {
        float mult = 1.0f;
        if (HasPerk(data, PerkId.StableGrip)) mult *= 0.7f;
        return mult;
    }

    public static float GetCritChance(WeaponData data)
    {
        float chance = 0.05f; // 基础 5% 暴击率
        if (HasPerk(data, PerkId.CriticalMaster)) chance += 0.15f;
        return chance;
    }

    public static float GetSpreadMultiplier(WeaponData data)
    {
        float mult = 1.0f;
        if (HasPerk(data, PerkId.ShotgunSpread)) mult *= 1.2f;
        return mult;
    }

    public static float GetBossDamageMultiplier(WeaponData data)
    {
        float mult = 1.0f;
        if (HasPerk(data, PerkId.HeadHunter)) mult *= 1.25f;
        return mult;
    }

    /// <summary>
    /// 随机生成 Perk（根据武器类型过滤可用 Perk）
    /// </summary>
    public static PerkId RollRandomPerk(WeaponType weaponType, List<PerkId> exclude = null)
    {
        var available = new List<PerkId>();
        foreach (var kvp in PerkWeaponRestriction)
        {
            if (kvp.Value.Contains(weaponType))
            {
                if (exclude == null || !exclude.Contains(kvp.Key))
                    available.Add(kvp.Key);
            }
        }

        if (available.Count == 0) return PerkId.KillClip; // fallback
        return available[GD.RandRange(0, available.Count - 1)];
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add project/scripts/weapon/PerkSystem.cs
git commit -m "feat(weapon): add perk system with 8 perks"
```

---

## Task 5: Hunter Class + Skills

**Goal:** Implement the Hunter class with Blink (Q), Mark (R), and Void Arrow Rain (F) abilities.

### Files:
- Create: `project/scripts/player/Hunter.cs`
- Modify: `project/scripts/player/Player.cs`

- [ ] **Step 1: Add skill/super infrastructure to Player.cs**

Add to Player.cs class body:

```csharp
// 技能系统
[Export] public float Skill1Cooldown = 8.0f;
[Export] public float Skill2Cooldown = 15.0f;
[Export] public float SuperChargePerKill = 5.0f;
[Export] public float SuperMaxCharge = 100.0f;

public float Skill1Timer { get; private set; }
public float Skill2Timer { get; private set; }
public float SuperCharge { get; private set; }
public bool IsSuperReady => SuperCharge >= SuperMaxCharge;

// 护盾
public float Shield { get; private set; }
public float MaxShield { get; private set; }

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
```

In `_Process`:

```csharp
public override void _Process(double delta)
{
    // 技能 CD 冷却
    if (Skill1Timer > 0) Skill1Timer -= (float)delta;
    if (Skill2Timer > 0) Skill2Timer -= (float)delta;
}
```

In `_UnhandledInput`, add:

```csharp
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
```

Add virtual methods:

```csharp
protected virtual void UseSkill1() { }
protected virtual void UseSkill2() { }
protected virtual void UseSuper() { }

public void AddSuperCharge(float amount)
{
    SuperCharge = Mathf.Min(SuperCharge + amount, SuperMaxCharge);
    EmitSignal(SignalName.SuperChargeChanged, SuperCharge, SuperMaxCharge);
}

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
```

In `Die()`, add kill super charge in `Enemy.Die()`:

```csharp
// 在 Enemy.Die() 中，找到 player 并充能
var players = GetTree().GetNodesInGroup("player");
if (players.Count > 0 && players[0] is Miao.Player.Player p)
{
    p.AddSuperCharge(p.SuperChargePerKill);
}
```

- [ ] **Step 2: Create Hunter.cs**

```csharp
using Godot;
using Miao.Weapon;

namespace Miao.Player;

public partial class Hunter : Player
{
    [Export] public PackedScene ArrowScene;

    public override void _Ready()
    {
        MoveSpeed = 240;
        MaxHealth = 80;
        MaxShield = 0; // 猎人无被动护盾
        Skill1Cooldown = 8.0f;
        Skill2Cooldown = 15.0f;
        base._Ready();
    }

    protected override void UseSkill1()
    {
        // 闪现：短距离瞬移，0.3秒无敌
        Skill1Timer = Skill1Cooldown;
        EmitSignal(SignalName.Skill1Used);

        var blinkDir = Velocity.Normalized();
        if (blinkDir == Vector2.Zero)
            blinkDir = GlobalTransform.X.Normalized(); // 默认朝瞄准方向

        GlobalPosition += blinkDir * 120; // 闪现距离

        // TODO: 添加无敌帧逻辑（0.3秒内 TakeDamage 不生效）
        // TODO: 闪现残影特效
    }

    protected override void UseSkill2()
    {
        // 标记：标记鼠标位置最近的敌人，受伤害+20%
        Skill2Timer = Skill2Cooldown;
        EmitSignal(SignalName.Skill2Used);

        var mousePos = GetGlobalMousePosition();
        Enemy.Enemy nearest = null;
        float nearestDist = 200f; // 标记范围

        foreach (var node in GetTree().GetNodesInGroup("enemy"))
        {
            if (node is Enemy.Enemy enemy)
            {
                float dist = enemy.GlobalPosition.DistanceTo(mousePos);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = enemy;
                }
            }
        }

        if (nearest != null)
        {
            nearest.ApplyMark(1.2f, 10.0f); // 伤害倍率，持续时间
        }
    }

    protected override void UseSuper()
    {
        // 虚空箭雨：扇形射出大量箭矢，穿透所有敌人
        SuperCharge = 0;
        EmitSignal(SignalName.SuperUsed);

        int arrowCount = 20;
        float totalSpread = Mathf.DegToRad(60); // 60度扇形
        float step = totalSpread / (arrowCount - 1);
        float startAngle = -totalSpread / 2;
        var fireDir = GlobalTransform.X.Normalized();

        for (int i = 0; i < arrowCount; i++)
        {
            float offset = startAngle + step * i;
            var dir = fireDir.Rotated(offset);

            var bullet = ArrowScene.Instantiate<Bullet>();
            bullet.GlobalPosition = GlobalPosition;
            bullet.Velocity = dir * 600;
            bullet.Damage = 30;
            bullet.CanPenetrate = true; // 穿透所有敌人
            bullet.Lifetime = 2.0f;
            GetTree().CurrentScene.AddChild(bullet);
        }
    }
}
```

- [ ] **Step 3: Add `ApplyMark` to Enemy.cs**

```csharp
// 在 Enemy.cs 中添加：
private float _markMultiplier = 1.0f;
private float _markTimer = 0;

public void ApplyMark(float multiplier, float duration)
{
    _markMultiplier = multiplier;
    _markTimer = duration;
}

// 在 TakeDamage 中使用：
public void TakeDamage(int damage)
{
    damage = Mathf.RoundToInt(damage * _markMultiplier);
    // ... 原有逻辑
}

// 在 _Process 中计时：
public override void _Process(double delta)
{
    if (_markTimer > 0)
    {
        _markTimer -= (float)delta;
        if (_markTimer <= 0) _markMultiplier = 1.0f;
    }
}
```

- [ ] **Step 4: Create Hunter scene**

Create `project/scenes/player/Hunter.tscn`:

```
Hunter (CharacterBody2D) [Hunter.cs]
  collision_layer = 1
  collision_mask = 1
  Sprite2D
  CollisionShape2D (RectangleShape2D 24x28)
  Camera2D (zoom 1.5)
  WeaponSlot (Marker2D)
```

- [ ] **Step 5: Commit**

```bash
git add project/scripts/player/ project/scenes/player/
git commit -m "feat(player): add Hunter class with Blink, Mark, and Void Arrow Rain"
```

---

## Task 6: Equipment Slot + Weapon Pickup

**Goal:** Implement single weapon slot, weapon pickup with equip/absorb choices.

### Files:
- Create: `project/scripts/weapon/EquipmentSlot.cs`
- Create: `project/scripts/ui/PickupPrompt.cs`
- Modify: `project/scripts/player/Player.cs`

- [ ] **Step 1: Create EquipmentSlot.cs**

```csharp
using Godot;
using System.Collections.Generic;

namespace Miao.Weapon;

public partial class EquipmentSlot : Node2D
{
    public Weapon CurrentWeapon { get; private set; }
    [Export] public PackedScene WeaponScene;

    public void EquipWeapon(WeaponData data)
    {
        // 移除旧武器
        if (CurrentWeapon != null)
        {
            CurrentWeapon.QueueFree();
        }

        // 创建新武器
        CurrentWeapon = WeaponScene.Instantiate<Weapon>();
        CurrentWeapon.SetWeaponData(data);
        AddChild(CurrentWeapon);
    }

    /// <summary>
    /// 吸收 Perk：将新武器的 Perk 转移到当前武器
    /// </summary>
    public bool TryAbsorbPerk(WeaponData newWeaponData)
    {
        if (CurrentWeapon?.Data == null) return false;
        if (newWeaponData.Perks.Count == 0) return false;

        var currentData = CurrentWeapon.Data;

        // 白武无 Perk 不可吸收
        if (newWeaponData.Rarity == Rarity.Common) return false;
        // 金武独特被动不可吸收
        if (newWeaponData.Rarity == Rarity.Legendary) return false;

        if (currentData.Perks.Count < currentData.MaxPerkSlots)
        {
            // 直接吸收第一个 Perk
            currentData.Perks.Add(newWeaponData.Perks[0]);
            return true;
        }
        else if (currentData.Perks.Count >= 2)
        {
            // 已满，需要选择替换（UI 层处理选择逻辑）
            return false; // 返回 false，让 UI 弹出选择
        }

        return false;
    }

    /// <summary>
    /// 替换指定位置的 Perk
    /// </summary>
    public void ReplacePerk(int slotIndex, PerkId newPerk)
    {
        if (CurrentWeapon?.Data == null) return;
        if (slotIndex < 0 || slotIndex >= CurrentWeapon.Data.Perks.Count) return;
        CurrentWeapon.Data.Perks[slotIndex] = newPerk;
    }

    public List<PerkId> GetCurrentPerks()
    {
        return CurrentWeapon?.Data?.Perks ?? new List<PerkId>();
    }
}
```

- [ ] **Step 2: Create PickupPrompt.cs — UI for weapon interaction**

```csharp
using Godot;

namespace Miao.UI;

public partial class PickupPrompt : CanvasLayer
{
    private Label _equipLabel;
    private Label _absorbLabel;
    private bool _isVisible;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;

        var vbox = new VBoxContainer();
        vbox.Position = new Vector2(640, 500);
        vbox.Alignment = BoxContainer.AlignmentMode.Center;

        _equipLabel = new Label();
        _equipLabel.Text = "[E] 装备";
        _equipLabel.HorizontalAlignment = HorizontalAlignment.Center;

        _absorbLabel = new Label();
        _absorbLabel.Text = "[E] 吸收Perk";
        _absorbLabel.HorizontalAlignment = HorizontalAlignment.Center;

        vbox.AddChild(_equipLabel);
        vbox.AddChild(_absorbLabel);
        AddChild(vbox);

        Hide();
    }

    public void ShowPrompt(bool canAbsorb)
    {
        _absorbLabel.Visible = canAbsorb;
        Show();
    }

    public new void Hide()
    {
        base.Hide();
    }
}
```

- [ ] **Step 3: Add weapon pickup logic to Player.cs**

Add to Player.cs:

```csharp
private WeaponData _nearbyWeaponData;
private Node2D _nearbyWeaponDrop;

public EquipmentSlot Equipment { get; private set; }

public override void _Ready()
{
    // ... existing code ...
    Equipment = GetNode<EquipmentSlot>("EquipmentSlot");
}

public override void _UnhandledInput(InputEvent @event)
{
    // ... existing shoot/skill logic ...

    if (@event.IsActionPressed("interact") && _nearbyWeaponData != null)
    {
        HandleWeaponPickup();
    }
}

private void HandleWeaponPickup()
{
    if (Equipment.CurrentWeapon == null)
    {
        // 没有武器，直接装备
        Equipment.EquipWeapon(_nearbyWeaponData);
        _nearbyWeaponDrop?.QueueFree();
        _nearbyWeaponData = null;
        _nearbyWeaponDrop = null;
    }
    else
    {
        // 有武器，弹出选择（UI 层处理）
        EmitSignal(SignalName.WeaponPickupChoice, _nearbyWeaponData);
    }
}

[Signal]
public delegate void WeaponPickupChoiceEventHandler(WeaponData newWeapon);
```

- [ ] **Step 4: Update Player.tscn**

Add `EquipmentSlot` (EquipmentSlot.cs) as child of Player, attached to `WeaponSlot` Marker2D.

- [ ] **Step 5: Commit**

```bash
git add project/scripts/weapon/EquipmentSlot.cs project/scripts/ui/PickupPrompt.cs project/scripts/player/Player.cs project/scenes/player/
git commit -m "feat(weapon): add equipment slot and weapon pickup system"
```

---

## Task 7: Loot Table + Enemy Loot Drops

**Goal:** Enemies drop weapons on death based on loot tables.

### Files:
- Create: `project/scripts/weapon/LootTable.cs`
- Modify: `project/scripts/enemy/Enemy.cs`

- [ ] **Step 1: Create LootTable.cs**

```csharp
using Godot;
using System.Collections.Generic;

namespace Miao.Weapon;

public static class LootTable
{
    /// <summary>
    /// 掉落武器的概率（基础）
    /// </summary>
    public const float BaseDropChance = 0.3f;

    /// <summary>
    /// 根据当前层数决定稀有度权重
    /// </summary>
    public static Rarity RollRarity(int roomIndex, bool isBoss)
    {
        if (isBoss)
        {
            // Boss: 10% 金，30% 紫，40% 蓝，20% 绿
            float roll = GD.Randf();
            if (roll < 0.10f) return Rarity.Legendary;
            if (roll < 0.40f) return Rarity.Epic;
            if (roll < 0.80f) return Rarity.Rare;
            return Rarity.Uncommon;
        }

        // 普通敌人：根据房间层数
        return roomIndex switch
        {
            0 => RollWeighted(new[] { (Rarity.Common, 60), (Rarity.Uncommon, 35), (Rarity.Rare, 5) }),
            1 => RollWeighted(new[] { (Rarity.Common, 40), (Rarity.Uncommon, 40), (Rarity.Rare, 15), (Rarity.Epic, 5) }),
            2 => RollWeighted(new[] { (Rarity.Uncommon, 30), (Rarity.Rare, 45), (Rarity.Epic, 20), (Rarity.Legendary, 5) }),
            3 => RollWeighted(new[] { (Rarity.Rare, 40), (Rarity.Epic, 40), (Rarity.Legendary, 20) }),
            _ => Rarity.Common
        };
    }

    private static Rarity RollWeighted((Rarity rarity, int weight)[] table)
    {
        int total = 0;
        foreach (var entry in table) total += entry.weight;

        int roll = GD.RandRange(0, total - 1);
        int cumulative = 0;
        foreach (var entry in table)
        {
            cumulative += entry.weight;
            if (roll < cumulative) return entry.rarity;
        }
        return table[^1].rarity;
    }

    /// <summary>
    /// 随机选择武器类型
    /// </summary>
    public static WeaponType RollWeaponType()
    {
        return (WeaponType)GD.RandRange(0, 2);
    }

    /// <summary>
    /// 生成一把随机武器
    /// </summary>
    public static WeaponData GenerateWeapon(int roomIndex, bool isBoss)
    {
        var rarity = RollRarity(roomIndex, isBoss);
        var type = RollWeaponType();

        var data = new WeaponData
        {
            Type = type,
            Rarity = rarity,
            DisplayName = GenerateName(type, rarity),
            Perks = new System.Collections.Generic.List<PerkId>()
        };

        // 设置基础属性
        switch (type)
        {
            case WeaponType.AutoRifle:
                data.BaseDamage = 8;
                data.FireRate = 6.0f;
                data.BulletSpeed = 500;
                data.KnockbackForce = 50;
                break;
            case WeaponType.Shotgun:
                data.BaseDamage = 15;
                data.FireRate = 1.5f;
                data.BulletSpeed = 400;
                data.BulletCount = 6;
                data.SpreadAngle = Mathf.DegToRad(30);
                data.KnockbackForce = 150;
                break;
            case WeaponType.HandCannon:
                data.BaseDamage = 25;
                data.FireRate = 2.5f;
                data.BulletSpeed = 600;
                data.KnockbackForce = 100;
                break;
        }

        // 稀有度加成
        float rarityMult = rarity switch
        {
            Rarity.Uncommon => 1.1f,
            Rarity.Rare => 1.25f,
            Rarity.Epic => 1.4f,
            Rarity.Legendary => 1.6f,
            _ => 1.0f
        };
        data.BaseDamage = Mathf.RoundToInt(data.BaseDamage * rarityMult);

        // Roll Perks
        int perkCount = data.MaxPerkSlots;
        var exclude = new List<PerkId>();
        for (int i = 0; i < perkCount; i++)
        {
            var perk = PerkSystem.RollRandomPerk(type, exclude);
            data.Perks.Add(perk);
            exclude.Add(perk);
        }

        return data;
    }

    private static string GenerateName(WeaponType type, Rarity rarity)
    {
        string prefix = rarity switch
        {
            Rarity.Common => "旧",
            Rarity.Uncommon => "制式",
            Rarity.Rare => "改良",
            Rarity.Epic => "精锐",
            Rarity.Legendary => "传说",
            _ => ""
        };

        string weapon = type switch
        {
            WeaponType.AutoRifle => "步枪",
            WeaponType.Shotgun => "霰弹枪",
            WeaponType.HandCannon => "手炮",
            _ => "武器"
        };

        return $"{prefix}{weapon}";
    }
}
```

- [ ] **Step 2: Add loot drop to Enemy.cs**

In `Enemy.Die()`:

```csharp
public void Die()
{
    EmitSignal(SignalName.EnemyDied, ExperienceValue);

    // 掉落经验球
    if (ExperienceOrbScene != null)
    {
        var orb = ExperienceOrbScene.Instantiate<Node2D>();
        orb.GlobalPosition = GlobalPosition;
        GetTree().CurrentScene.AddChild(orb);
    }

    // 掉落武器
    if (GD.Randf() < LootTable.BaseDropChance)
    {
        var weaponData = LootTable.GenerateWeapon(_roomIndex, _isElite);
        SpawnWeaponDrop(weaponData);
    }

    // 掉落微光（金币）
    EmitSignal(SignalName.GlimmerDropped, _isElite ? 20 : 5);

    QueueFree();
}

private void SpawnWeaponDrop(WeaponData data)
{
    // 创建地面武器掉落物
    var drop = new Area2D();
    drop.CollisionLayer = 4; // 拾取层
    drop.CollisionMask = 1;  // 检测玩家

    var shape = new CircleShape2D();
    shape.Radius = 16;
    var collision = new CollisionShape2D();
    collision.Shape = shape;
    drop.AddChild(collision);

    // 稀有度颜色光效
    var color = data.Rarity switch
    {
        Rarity.Common => new Color(0.7f, 0.7f, 0.7f),
        Rarity.Uncommon => new Color(0.2f, 0.8f, 0.2f),
        Rarity.Rare => new Color(0.2f, 0.4f, 1.0f),
        Rarity.Epic => new Color(0.6f, 0.2f, 0.8f),
        Rarity.Legendary => new Color(1.0f, 0.8f, 0.0f),
        _ => Colors.White
    };

    var sprite = new Sprite2D();
    // TODO: 使用武器类型对应的贴图
    sprite.Modulate = color;
    drop.AddChild(sprite);

    drop.GlobalPosition = GlobalPosition + new Vector2(GD.RandRange(-20, 20), GD.RandRange(-20, 20));

    // 存储武器数据到 metadata
    drop.SetMeta("weapon_data", data);

    // 玩家进入范围时显示拾取提示
    drop.BodyEntered += (body) =>
    {
        if (body is Miao.Player.Player player)
        {
            player.SetNearbyWeapon(data, drop);
        }
    };
    drop.BodyExited += (body) =>
    {
        if (body is Miao.Player.Player player)
        {
            player.ClearNearbyWeapon();
        }
    };

    GetTree().CurrentScene.AddChild(drop);
}
```

Add to Enemy.cs:

```csharp
[Export] public int RoomIndex { get; set; }
[Export] public bool IsElite { get; set; }

[Signal]
public delegate void GlimmerDroppedEventHandler(int amount);
```

- [ ] **Step 3: Commit**

```bash
git add project/scripts/weapon/LootTable.cs project/scripts/enemy/Enemy.cs
git commit -m "feat(loot): add loot table and enemy weapon drops"
```

---

## Task 8: Elite Enemy Modifiers

**Goal:** Add elite enemies with random modifiers (speed, shield, split, regen).

### Files:
- Create: `project/scripts/enemy/EliteModifier.cs`
- Modify: `project/scripts/enemy/Enemy.cs`

- [ ] **Step 1: Create EliteModifier.cs**

```csharp
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
                // 回血在 _Process 中处理
                break;
            case EliteModType.Split:
                // 分裂在 Die 中处理
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
                _regenTimer = 1.0f; // 每秒回血
                _enemy.Heal(Mathf.RoundToInt(_enemy.MaxHealth * 0.02f)); // 每秒回 2%
            }
        }
    }

    public void OnDeath()
    {
        if (Type == EliteModType.Split)
        {
            // 分裂：生成 2 个小型敌人
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
```

- [ ] **Step 2: Add shield and heal to Enemy.cs**

```csharp
private float _shield;

public void AddShield(float amount)
{
    _shield += amount;
}

public void Heal(int amount)
{
    CurrentHealth = Mathf.Min(CurrentHealth + amount, MaxHealth);
}

// 修改 TakeDamage：
public void TakeDamage(int damage)
{
    // 先扣护盾
    if (_shield > 0)
    {
        float absorbed = Mathf.Min(_shield, damage);
        _shield -= absorbed;
        damage -= Mathf.RoundToInt(absorbed);
    }

    // 应用标记加成
    damage = Mathf.RoundToInt(damage * _markMultiplier);

    CurrentHealth -= damage;
    CurrentHealth = Mathf.Max(CurrentHealth, 0);

    if (CurrentHealth <= 0) Die();
}
```

- [ ] **Step 3: Commit**

```bash
git add project/scripts/enemy/EliteModifier.cs project/scripts/enemy/Enemy.cs
git commit -m "feat(enemy): add elite modifiers (speed/shield/split/regen)"
```

---

## Task 9: Room System — Room Generator + Wave Spawning

**Goal:** Replace infinite spawning with room-based progression (4 rooms + 1 boss).

### Files:
- Create: `project/scripts/system/RoomGenerator.cs`
- Modify: `project/scripts/system/SpawnManager.cs`
- Modify: `project/scripts/system/GameManager.cs`

- [ ] **Step 1: Create RoomGenerator.cs**

```csharp
using Godot;
using System.Collections.Generic;
using Miao.Enemy;

namespace Miao.System;

public partial class RoomGenerator : Node
{
    [Export] public PackedScene SmallBugScene;
    [Export] public PackedScene FastBugScene;
    [Export] public PackedScene TankBugScene;
    [Export] public PackedScene EliteBugScene;

    public int CurrentRoom { get; private set; }
    public int TotalRooms => 5; // 4普通 + 1Boss
    public bool IsBossRoom => CurrentRoom == 4;

    // 房间波次定义
    private readonly int[][] _waveCounts = new int[][]
    {
        new[] { 8, 10, 12 },           // 房间1: 3波
        new[] { 10, 12, 14, 16 },      // 房间2: 4波
        new[] { 8, 10, 6, 8, 10 },     // 房间3: 5波（含精英）
        new[] { 12, 14, 10, 12, 16 },  // 房间4: 5波
    };

    private int _currentWave;
    private int _enemiesRemaining;
    private Node2D _enemyContainer;
    private Node2D _player;

    [Signal]
    public delegate void RoomClearedEventHandler(int roomIndex);
    [Signal]
    public delegate void WaveStartedEventHandler(int waveIndex);
    [Signal]
    public delegate void AllRoomsClearedEventHandler();

    public override void _Ready()
    {
        _enemyContainer = new Node2D();
        _enemyContainer.Name = "Enemies";
        GetTree().CurrentScene.AddChild(_enemyContainer);
    }

    public void StartRun(Node2D player)
    {
        _player = player;
        CurrentRoom = 0;
        StartRoom();
    }

    private void StartRoom()
    {
        _currentWave = 0;
        GD.Print($"进入房间 {CurrentRoom + 1}/{TotalRooms}");
        SpawnWave();
    }

    private void SpawnWave()
    {
        if (IsBossRoom)
        {
            SpawnBoss();
            return;
        }

        var waves = _waveCounts[CurrentRoom];
        if (_currentWave >= waves.Length)
        {
            // 房间清空
            EmitSignal(SignalName.RoomCleared, CurrentRoom);
            return;
        }

        int count = waves[_currentWave];
        bool hasElite = (CurrentRoom == 2 && _currentWave >= 2); // 房间3有精英

        EmitSignal(SignalName.WaveStarted, _currentWave);

        for (int i = 0; i < count; i++)
        {
            var scene = ChooseEnemyType();
            var enemy = scene.Instantiate<Enemy>();
            enemy.GlobalPosition = GetSpawnPosition();
            enemy.RoomIndex = CurrentRoom;

            if (hasElite && i == 0)
            {
                enemy.IsElite = true;
                var mod = new EliteModifier();
                mod.Init(enemy, (EliteModType)GD.RandRange(0, 3));
                enemy.AddChild(mod);
            }

            enemy.EnemyDied += OnEnemyDied;
            _enemyContainer.AddChild(enemy);
            _enemiesRemaining++;
        }

        _currentWave++;
    }

    private void OnEnemyDied(int experience)
    {
        _enemiesRemaining--;
        if (_enemiesRemaining <= 0)
        {
            // 当前波清理完毕，延迟后生成下一波
            GetTree().CreateTimer(2.0).Timeout += SpawnWave;
        }
    }

    private void SpawnBoss()
    {
        // TODO: 实现 Boss 生成
        GD.Print("Boss 战开始！");
    }

    private PackedScene ChooseEnemyType()
    {
        float roll = GD.Randf();
        if (CurrentRoom < 2)
        {
            return roll < 0.7f ? SmallBugScene : FastBugScene;
        }
        else
        {
            if (roll < 0.4f) return SmallBugScene;
            if (roll < 0.7f) return FastBugScene;
            return TankBugScene;
        }
    }

    private Vector2 GetSpawnPosition()
    {
        // 在玩家周围一定距离随机生成
        float distance = 400;
        float angle = GD.Randf() * Mathf.Tau;
        return _player.GlobalPosition + new Vector2(
            Mathf.Cos(angle) * distance,
            Mathf.Sin(angle) * distance
        );
    }

    public void AdvanceRoom()
    {
        CurrentRoom++;
        if (CurrentRoom >= TotalRooms)
        {
            EmitSignal(SignalName.AllRoomsCleared);
        }
        else
        {
            StartRoom();
        }
    }
}
```

- [ ] **Step 2: Update GameManager.cs for room-based flow**

```csharp
using Godot;
using Miao.Player;

namespace Miao.System;

public partial class GameManager : Node
{
    public static GameManager Instance { get; private set; }

    public enum GameState { MainMenu, Playing, RoomTransition, Boss, Settlement, GameOver }
    public GameState CurrentState { get; private set; }

    private Player _player;
    private RoomGenerator _roomGenerator;
    private float _gameTime;
    private int _killCount;
    private int _glimmer;

    [Signal]
    public delegate void GlimmerChangedEventHandler(int amount);

    public override void _Ready()
    {
        Instance = this;
    }

    public override void _Process(double delta)
    {
        if (CurrentState == GameState.Playing)
        {
            _gameTime += (float)delta;
        }
    }

    public void StartGame()
    {
        CurrentState = GameState.Playing;
        _gameTime = 0;
        _killCount = 0;
        GetTree().ChangeSceneToFile("res://scenes/main.tscn");
    }

    public void RegisterPlayer(Player player)
    {
        _player = player;
        player.PlayerDied += OnPlayerDied;
    }

    public void RegisterRoomGenerator(RoomGenerator rg)
    {
        _roomGenerator = rg;
        rg.RoomCleared += OnRoomCleared;
        rg.AllRoomsCleared += OnAllRoomsCleared;
        rg.StartRun(_player);
    }

    private void OnRoomCleared(int roomIndex)
    {
        // 房间清空，进入过渡
        CurrentState = GameState.RoomTransition;
        GD.Print($"房间 {roomIndex + 1} 清空！按 E 进入下一房间");

        // TODO: 显示房间奖励 UI
    }

    public void NextRoom()
    {
        CurrentState = GameState.Playing;
        _roomGenerator.AdvanceRoom();
    }

    private void OnAllRoomsCleared()
    {
        // 所有房间清空，进入结算
        CurrentState = GameState.Settlement;
        GD.Print("所有房间清空！进入结算");
        // TODO: 显示结算界面
    }

    private void OnPlayerDied()
    {
        CurrentState = GameState.GameOver;
        GetTree().Paused = true;
        // TODO: 显示 GameOver UI
    }

    public void AddGlimmer(int amount)
    {
        _glimmer += amount;
        EmitSignal(SignalName.GlimmerChanged, _glimmer);
    }

    public int GetGlimmer() => _glimmer;
    public float GetGameTime() => _gameTime;
    public int GetKillCount() => _killCount;
}
```

- [ ] **Step 3: Remove old SpawnManager.cs** (replaced by RoomGenerator)

```bash
rm project/scripts/system/SpawnManager.cs
```

- [ ] **Step 4: Commit**

```bash
git add project/scripts/system/RoomGenerator.cs project/scripts/system/GameManager.cs project/scripts/system/SpawnManager.cs
git commit -m "feat(system): replace infinite spawning with room-based progression"
```

---

## Task 10: Boss — Bug Queen (3 Phases)

**Goal:** Implement the Bug Queen boss with 3 phases.

### Files:
- Create: `project/scripts/enemy/BugQueen.cs`
- Create: `project/scenes/enemy/BugQueen.tscn`

- [ ] **Step 1: Create BugQueen.cs**

```csharp
using Godot;
using System.Collections.Generic;

namespace Miao.Enemy;

public partial class BugQueen : Enemy
{
    public enum BossPhase { Phase1, Phase2, Phase3 }

    [Export] public PackedScene SmallBugScene;
    [Export] public PackedScene PoisonScene;

    private BossPhase _currentPhase = BossPhase.Phase1;
    private float _attackTimer;
    private float _summonTimer;
    private float _phaseTransitionTimer;
    private bool _isTransitioning;
    private List<Node2D> _poisonPools = new();

    public override void _Ready()
    {
        MaxHealth = 500;
        CurrentHealth = MaxHealth;
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

        // 检查阶段转换
        CheckPhaseTransition();
    }

    private void UpdatePhase1(double delta)
    {
        // 召唤小虫
        if (_summonTimer <= 0)
        {
            _summonTimer = 4.0f;
            SummonSmallBugs(3);
        }

        // 地面毒液攻击
        if (_attackTimer <= 0)
        {
            _attackTimer = 3.0f;
            SpawnPoisonPool();
        }
    }

    private void UpdatePhase2(double delta)
    {
        // 冲锋攻击
        if (_attackTimer <= 0)
        {
            _attackTimer = 3.5f;
            ChargeAttack();
        }

        // 召唤精英虫
        if (_summonTimer <= 0)
        {
            _summonTimer = 6.0f;
            SummonElite();
        }
    }

    private void UpdatePhase3(double delta)
    {
        // 狂暴：攻击速度加快
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

        // 全屏毒液逐渐收缩（通过增大毒液池实现）
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
        _phaseTransitionTimer = 2.0f; // 2秒硬直
        GD.Print($"虫后硬直！输出窗口！");

        // 清除所有毒液池
        foreach (var pool in _poisonPools)
        {
            if (IsInstanceValid(pool)) pool.QueueFree();
        }
        _poisonPools.Clear();
    }

    private void SummonSmallBugs(int count)
    {
        for (int i = 0; i < count; i++)
        {
            var bug = SmallBugScene.Instantiate<Enemy>();
            bug.GlobalPosition = GlobalPosition + new Vector2(GD.RandRange(-50, 50), GD.RandRange(-50, 50));
            GetTree().CurrentScene.AddChild(bug);
        }
    }

    private void SummonElite()
    {
        var bug = SmallBugScene.Instantiate<Enemy>();
        bug.GlobalPosition = GlobalPosition + new Vector2(GD.RandRange(-50, 50), GD.RandRange(-50, 50));
        bug.IsElite = true;
        var mod = new EliteModifier();
        mod.Init(bug, EliteModType.Shield);
        bug.AddChild(mod);
        GetTree().CurrentScene.AddChild(bug);
    }

    private void SpawnPoisonPool()
    {
        // 在玩家位置生成毒液池
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

        // 绿色半透明视觉
        var colorRect = new ColorRect();
        colorRect.Size = new Vector2(80, 80);
        colorRect.Position = new Vector2(-40, -40);
        colorRect.Color = new Color(0, 0.5f, 0, 0.4f);
        pool.AddChild(colorRect);

        pool.GlobalPosition = ((Node2D)players[0]).GlobalPosition;

        // 持续伤害
        pool.BodyEntered += (body) =>
        {
            if (body is Miao.Player.Player player)
            {
                player.TakeDamage(5);
            }
        };

        GetTree().CurrentScene.AddChild(pool);
        _poisonPools.Add(pool);

        // 8秒后消失
        GetTree().CreateTimer(8.0).Timeout += () =>
        {
            if (IsInstanceValid(pool)) pool.QueueFree();
            _poisonPools.Remove(pool);
        };
    }

    private void ChargeAttack()
    {
        // 向玩家方向冲锋
        var players = GetTree().GetNodesInGroup("player");
        if (players.Count == 0) return;

        var target = ((Node2D)players[0]).GlobalPosition;
        var direction = (target - GlobalPosition).Normalized();

        // 快速移动
        var tween = CreateTween();
        tween.TweenProperty(this, "global_position", GlobalPosition + direction * 300, 0.5f);
    }
}
```

- [ ] **Step 2: Create BugQueen.tscn**

```
BugQueen (CharacterBody2D) [BugQueen.cs]
  collision_layer = 2
  collision_mask = 1
  MoveSpeed = 60
  MaxHealth = 500
  ContactDamage = 25
  Sprite2D (scale 2x for boss size)
  CollisionShape2D (RectangleShape2D 40x40)
```

- [ ] **Step 3: Integrate boss into RoomGenerator**

In `RoomGenerator.SpawnBoss()`:

```csharp
private void SpawnBoss()
{
    EmitSignal(SignalName.WaveStarted, -1); // -1 = boss

    var boss = GD.Load<PackedScene>("res://scenes/enemy/BugQueen.tscn").Instantiate<BugQueen>();
    boss.GlobalPosition = _player.GlobalPosition + new Vector2(0, -300);
    boss.EnemyDied += OnBossDied;
    _enemyContainer.AddChild(boss);
}

private void OnBossDied(int experience)
{
    GD.Print("虫后被击败！");
    EmitSignal(SignalName.RoomCleared, CurrentRoom);
    EmitSignal(SignalName.AllRoomsCleared);
}
```

- [ ] **Step 4: Commit**

```bash
git add project/scripts/enemy/BugQueen.cs project/scenes/enemy/BugQueen.tscn project/scripts/system/RoomGenerator.cs
git commit -m "feat(boss): add Bug Queen boss with 3 phases"
```

---

## Task 11: Meta Progression — Glimmer + Permanent Upgrades

**Goal:** Implement permanent upgrades with Glimmer currency.

### Files:
- Modify: `project/scripts/system/MetaProgression.cs`

- [ ] **Step 1: Rewrite MetaProgression.cs**

```csharp
using Godot;

namespace Miao.System;

public partial class MetaProgression : Node
{
    public static MetaProgression Instance { get; private set; }

    // 持久化数据
    public int Glimmer { get; private set; }
    public int BaseHealthLevel { get; private set; }
    public int BaseDamageLevel { get; private set; }
    public int MoveSpeedLevel { get; private set; }
    public int DropRateLevel { get; private set; }
    public int StartWeaponLevel { get; private set; }

    // 升级定义
    private readonly int[] _upgradeCosts = { 100, 200, 400, 800, 1600, 3200, 6400, 12800, 25600, 51200 };

    public override void _Ready()
    {
        Instance = this;
        Load();
    }

    // Meta 升级效果
    public int GetBonusHealth() => BaseHealthLevel * 10;
    public float GetBonusDamage() => 1.0f + BaseDamageLevel * 0.05f;
    public float GetBonusMoveSpeed() => 1.0f + MoveSpeedLevel * 0.03f;
    public float GetBonusDropRate() => 1.0f + DropRateLevel * 0.05f;

    public bool CanAffordUpgrade(int currentLevel, int maxLevel)
    {
        if (currentLevel >= maxLevel) return false;
        return Glimmer >= _upgradeCosts[currentLevel];
    }

    public bool TryUpgrade(string upgradeId, int maxLevel)
    {
        int currentLevel = upgradeId switch
        {
            "base_health" => BaseHealthLevel,
            "base_damage" => BaseDamageLevel,
            "move_speed" => MoveSpeedLevel,
            "drop_rate" => DropRateLevel,
            "start_weapon" => StartWeaponLevel,
            _ => 0
        };

        if (!CanAffordUpgrade(currentLevel, maxLevel)) return false;

        int cost = _upgradeCosts[currentLevel];
        Glimmer -= cost;

        switch (upgradeId)
        {
            case "base_health": BaseHealthLevel++; break;
            case "base_damage": BaseDamageLevel++; break;
            case "move_speed": MoveSpeedLevel++; break;
            case "drop_rate": DropRateLevel++; break;
            case "start_weapon": StartWeaponLevel++; break;
        }

        Save();
        return true;
    }

    public void AddGlimmer(int amount)
    {
        Glimmer += amount;
        Save();
    }

    public void Save()
    {
        var config = new ConfigFile();
        config.SetValue("meta", "glimmer", Glimmer);
        config.SetValue("meta", "base_health_level", BaseHealthLevel);
        config.SetValue("meta", "base_damage_level", BaseDamageLevel);
        config.SetValue("meta", "move_speed_level", MoveSpeedLevel);
        config.SetValue("meta", "drop_rate_level", DropRateLevel);
        config.SetValue("meta", "start_weapon_level", StartWeaponLevel);
        config.Save("user://meta_progress.cfg");
    }

    public void Load()
    {
        var config = new ConfigFile();
        if (config.Load("user://meta_progress.cfg") != Error.Ok) return;

        Glimmer = (int)config.GetValue("meta", "glimmer", 0);
        BaseHealthLevel = (int)config.GetValue("meta", "base_health_level", 0);
        BaseDamageLevel = (int)config.GetValue("meta", "base_damage_level", 0);
        MoveSpeedLevel = (int)config.GetValue("meta", "move_speed_level", 0);
        DropRateLevel = (int)config.GetValue("meta", "drop_rate_level", 0);
        StartWeaponLevel = (int)config.GetValue("meta", "start_weapon_level", 0);
    }
}
```

- [ ] **Step 2: Connect Glimmer drops to MetaProgression**

In `Enemy.Die()`, after emitting `GlimmerDropped`:

```csharp
// 掉落微光
int glimmerAmount = _isElite ? 20 : 5;
MetaProgression.Instance.AddGlimmer(glimmerAmount);
```

- [ ] **Step 3: Commit**

```bash
git add project/scripts/system/MetaProgression.cs project/scripts/enemy/Enemy.cs
git commit -m "feat(meta): add glimmer currency and permanent upgrades"
```

---

## Task 12: Collection Codex

**Goal:** Track discovered weapons and perks, provide collection bonuses.

### Files:
- Create: `project/scripts/system/CollectionCodex.cs`

- [ ] **Step 1: Create CollectionCodex.cs**

```csharp
using Godot;
using System.Collections.Generic;
using Miao.Weapon;

namespace Miao.System;

public partial class CollectionCodex : Node
{
    public static CollectionCodex Instance { get; private set; }

    public HashSet<string> DiscoveredWeapons { get; private set; } = new();
    public HashSet<PerkId> DiscoveredPerks { get; private set; } = new();
    public int TotalDiscoveries => DiscoveredWeapons.Count + DiscoveredPerks.Count;

    // 每解锁 10 个条目，+2% 全局伤害
    public float CollectionDamageBonus => 1.0f + (TotalDiscoveries / 10) * 0.02f;

    public override void _Ready()
    {
        Instance = this;
        Load();
    }

    public void RegisterWeapon(WeaponData data)
    {
        string key = $"{data.Type}_{data.Rarity}";
        if (DiscoveredWeapons.Add(key))
        {
            GD.Print($"图鉴解锁：{data.DisplayName}");
            Save();
        }

        foreach (var perk in data.Perks)
        {
            if (DiscoveredPerks.Add(perk))
            {
                GD.Print($"图鉴解锁 Perk：{PerkSystem.PerkInfo[perk].Name}");
            }
        }

        Save();
    }

    public void Save()
    {
        var config = new ConfigFile();

        var weaponList = new string[DiscoveredWeapons.Count];
        DiscoveredWeapons.CopyTo(weaponList);
        config.SetValue("codex", "weapons", string.Join(",", weaponList));

        var perkList = new string[DiscoveredPerks.Count];
        int i = 0;
        foreach (var p in DiscoveredPerks) perkList[i++] = p.ToString();
        config.SetValue("codex", "perks", string.Join(",", perkList));

        config.Save("user://collection_codex.cfg");
    }

    public void Load()
    {
        var config = new ConfigFile();
        if (config.Load("user://collection_codex.cfg") != Error.Ok) return;

        var weapons = config.GetValue("codex", "weapons", "").ToString();
        if (!string.IsNullOrEmpty(weapons))
        {
            foreach (var w in weapons.Split(','))
                DiscoveredWeapons.Add(w);
        }

        var perks = config.GetValue("codex", "perks", "").ToString();
        if (!string.IsNullOrEmpty(perks))
        {
            foreach (var p in perks.Split(','))
            {
                if (System.Enum.TryParse<PerkId>(p, out var perkId))
                    DiscoveredPerks.Add(perkId);
            }
        }
    }
}
```

- [ ] **Step 2: Register weapons when picked up**

In `EquipmentSlot.EquipWeapon()`:

```csharp
public void EquipWeapon(WeaponData data)
{
    // ... existing code ...
    CollectionCodex.Instance?.RegisterWeapon(data);
}
```

- [ ] **Step 3: Apply collection bonus to damage**

In `Weapon.EffectiveDamage`:

```csharp
public int EffectiveDamage
{
    get
    {
        float bonus = CollectionCodex.Instance?.CollectionDamageBonus ?? 1.0f;
        return Mathf.RoundToInt(Data.BaseDamage * PerkSystem.GetDamageMultiplier(Data) * bonus);
    }
}
```

- [ ] **Step 4: Commit**

```bash
git add project/scripts/system/CollectionCodex.cs project/scripts/weapon/EquipmentSlot.cs project/scripts/weapon/Weapon.cs
git commit -m "feat(codex): add collection codex with discovery tracking and damage bonus"
```

---

## Task 13: UI Overhaul — HUD + Settlement + Lobby

**Goal:** Redesign HUD for looter-shooter, add settlement screen, update lobby.

### Files:
- Modify: `project/scripts/ui/HUD.cs`
- Modify: `project/scripts/ui/GameOverUI.cs` → rename to `SettlementUI.cs`
- Modify: `project/scripts/ui/MainMenu.cs`
- Create: `project/scripts/ui/SkillCooldownUI.cs`
- Create: `project/scripts/ui/WeaponInfoUI.cs`

- [ ] **Step 1: Redesign HUD.cs**

```csharp
using Godot;
using Miao.Player;

namespace Miao.UI;

public partial class HUD : CanvasLayer
{
    private ProgressBar _healthBar;
    private ProgressBar _shieldBar;
    private ProgressBar _superBar;
    private Label _weaponLabel;
    private Label _perksLabel;
    private Label _skill1Label;
    private Label _skill2Label;
    private Label _superLabel;
    private Player _player;

    public override void _Ready()
    {
        // 光等条（左上）
        _healthBar = CreateBar(new Vector2(20, 20), new Vector2(200, 20), Colors.Red);
        _shieldBar = CreateBar(new Vector2(20, 45), new Vector2(200, 12), Colors.Cyan);

        // 超能条（底部中央）
        _superBar = CreateBar(new Vector2(540, 680), new Vector2(200, 15), new Color(1, 0.8f, 0));

        // 武器信息（右下）
        _weaponLabel = CreateLabel(new Vector2(1050, 660), 14);
        _perksLabel = CreateLabel(new Vector2(1050, 680), 12);

        // 技能图标（左下）
        _skill1Label = CreateLabel(new Vector2(20, 660), 16);
        _skill2Label = CreateLabel(new Vector2(80, 660), 16);
        _superLabel = CreateLabel(new Vector2(540, 700), 12);
    }

    public void SetPlayer(Player player)
    {
        _player = player;
        player.HealthChanged += OnHealthChanged;
        player.ShieldChanged += OnShieldChanged;
        player.SuperChargeChanged += OnSuperChargeChanged;
    }

    public override void _Process(double delta)
    {
        if (_player == null) return;

        // 更新技能 CD
        _skill1Label.Text = _player.Skill1Timer > 0
            ? $"Q: {_player.Skill1Timer:F1}s"
            : "Q: 就绪";
        _skill2Label.Text = _player.Skill2Timer > 0
            ? $"R: {_player.Skill2Timer:F1}s"
            : "R: 就绪";
        _superLabel.Text = _player.IsSuperReady
            ? "F: 超能就绪！"
            : $"F: {_player.SuperCharge:F0}/{_player.SuperMaxCharge:F0}";

        // 更新武器信息
        var weapon = _player.Equipment?.CurrentWeapon?.Data;
        if (weapon != null)
        {
            string rarityColor = weapon.Rarity switch
            {
                Weapon.Rarity.Common => "白",
                Weapon.Rarity.Uncommon => "绿",
                Weapon.Rarity.Rare => "蓝",
                Weapon.Rarity.Epic => "紫",
                Weapon.Rarity.Legendary => "金",
                _ => ""
            };
            _weaponLabel.Text = $"[{rarityColor}] {weapon.DisplayName}";
            _perksLabel.Text = string.Join(" | ", weapon.Perks.ConvertAll(p => Weapon.PerkSystem.PerkInfo[p].Name));
        }
        else
        {
            _weaponLabel.Text = "无武器";
            _perksLabel.Text = "";
        }
    }

    private void OnHealthChanged(int current, int max)
    {
        _healthBar.MaxValue = max;
        _healthBar.Value = current;
    }

    private void OnShieldChanged(float current, float max)
    {
        _shieldBar.MaxValue = max;
        _shieldBar.Value = current;
    }

    private void OnSuperChargeChanged(float current, float max)
    {
        _superBar.MaxValue = max;
        _superBar.Value = current;
    }

    private ProgressBar CreateBar(Vector2 pos, Vector2 size, Color color)
    {
        var bar = new ProgressBar();
        bar.Position = pos;
        bar.Size = size;
        bar.MaxValue = 100;
        bar.Value = 100;
        bar.ShowPercentage = false;
        // TODO: 设置颜色主题
        AddChild(bar);
        return bar;
    }

    private Label CreateLabel(Vector2 pos, int fontSize)
    {
        var label = new Label();
        label.Position = pos;
        // TODO: 设置字体大小
        AddChild(label);
        return label;
    }
}
```

- [ ] **Step 2: Create SettlementUI.cs (replaces GameOverUI)**

```csharp
using Godot;
using Miao.System;
using Miao.Weapon;

namespace Miao.UI;

public partial class SettlementUI : CanvasLayer
{
    private VBoxContainer _weaponList;
    private Label _statsLabel;
    private Label _glimmerLabel;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;

        var panel = new PanelContainer();
        panel.Size = new Vector2(600, 400);
        panel.Position = new Vector2(340, 160);

        var vbox = new VBoxContainer();

        var title = new Label();
        title.Text = "任务结算";
        title.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(title);

        _statsLabel = new Label();
        vbox.AddChild(_statsLabel);

        _glimmerLabel = new Label();
        vbox.AddChild(_glimmerLabel);

        _weaponList = new VBoxContainer();
        vbox.AddChild(_weaponList);

        // 按钮
        var buttonBox = new HBoxContainer();
        buttonBox.Alignment = BoxContainer.AlignmentMode.Center;

        var restartBtn = new Button();
        restartBtn.Text = "再来一局";
        restartBtn.Pressed += () => GameManager.Instance.StartGame();
        buttonBox.AddChild(restartBtn);

        var menuBtn = new Button();
        menuBtn.Text = "返回大厅";
        menuBtn.Pressed += () => GameManager.Instance.ReturnToMainMenu();
        buttonBox.AddChild(menuBtn);

        vbox.AddChild(buttonBox);
        panel.AddChild(vbox);
        AddChild(panel);
    }

    public void SetRunStats(float time, int kills, int glimmerEarned)
    {
        int minutes = (int)(time / 60);
        int seconds = (int)(time % 60);
        _statsLabel.Text = $"击杀: {kills} | 用时: {minutes}:{seconds:D2}";
        _glimmerLabel.Text = $"获得微光: {glimmerEarned}";
    }
}
```

- [ ] **Step 3: Update MainMenu.cs for class selection**

```csharp
using Godot;
using Miao.System;

namespace Miao.UI;

public partial class MainMenu : Control
{
    public override void _Ready()
    {
        var vbox = new VBoxContainer();
        vbox.Alignment = BoxContainer.AlignmentMode.Center;
        vbox.Position = new Vector2(540, 200);

        var title = new Label();
        title.Text = "MIAO";
        title.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(title);

        var subtitle = new Label();
        subtitle.Text = "命运·像素";
        subtitle.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(subtitle);

        // 职业选择
        var classLabel = new Label();
        classLabel.Text = "\n选择职业：";
        vbox.AddChild(classLabel);

        var hunterBtn = new Button();
        hunterBtn.Text = "猎人（敏捷远程）";
        hunterBtn.Pressed += () => StartWithClass("hunter");
        vbox.AddChild(hunterBtn);

        var titanBtn = new Button();
        titanBtn.Text = "泰坦（近战坦克）";
        titanBtn.Disabled = true; // MVP 只开放猎人
        vbox.AddChild(titanBtn);

        var warlockBtn = new Button();
        warlockBtn.Text = "术士（AOE法师）";
        warlockBtn.Disabled = true;
        vbox.AddChild(warlockBtn);

        // Meta 升级按钮
        var metaBtn = new Button();
        metaBtn.Text = "Meta 升级";
        metaBtn.Pressed += () => { /* TODO: 打开 Meta 升级界面 */ };
        vbox.AddChild(metaBtn);

        // 图鉴按钮
        var codexBtn = new Button();
        codexBtn.Text = "收藏图鉴";
        codexBtn.Pressed += () => { /* TODO: 打开图鉴界面 */ };
        vbox.AddChild(codexBtn);

        var quitBtn = new Button();
        quitBtn.Text = "退出";
        quitBtn.Pressed += GetTree().Quit;
        vbox.AddChild(quitBtn);

        AddChild(vbox);
    }

    private void StartWithClass(string classId)
    {
        // TODO: 设置选中职业，开始游戏
        GameManager.Instance.StartGame();
    }
}
```

- [ ] **Step 4: Commit**

```bash
git add project/scripts/ui/
git commit -m "feat(ui): redesign HUD for looter-shooter, add settlement and class selection"
```

---

## Task 14: Integration — Wire Everything Together

**Goal:** Connect all systems in the main scene.

### Files:
- Modify: `project/scenes/main.tscn`
- Create: `project/scripts/system/Main.cs` (update)

- [ ] **Step 1: Update main.tscn structure**

```
Main (Node2D) [Main.cs]
  MapBackground (Node2D)
  Hunter (instance of Hunter.tscn) at (640, 360)
  RoomGenerator (Node) [RoomGenerator.cs]
    SmallBugScene, FastBugScene, TankBugScene assigned
  HUD (instance of HUD.tscn)
  MapBoundary (Node2D)
```

- [ ] **Step 2: Update Main.cs to wire systems**

```csharp
using Godot;
using Miao.Player;

namespace Miao.System;

public partial class Main : Node2D
{
    public override void _Ready()
    {
        var player = GetNode<Hunter>("Hunter");
        GameManager.Instance.RegisterPlayer(player);

        var hud = GetNode<UI.HUD>("HUD");
        hud.SetPlayer(player);

        var roomGen = GetNode<RoomGenerator>("RoomGenerator");
        GameManager.Instance.RegisterRoomGenerator(roomGen);
    }
}
```

- [ ] **Step 3: Commit**

```bash
git add project/scenes/main.tscn project/scripts/system/Main.cs
git commit -m "feat(integration): wire all systems in main scene"
```

---

## Summary

| Task | Description | Est. Time |
|------|-------------|-----------|
| 1 | Project setup — merge roguelike branch | 15 min |
| 2 | Player aiming — free aim with mouse | 30 min |
| 3 | Weapon system overhaul — rarity + perks + shooting | 45 min |
| 4 | Perk system — 8 perks with effects | 30 min |
| 5 | Hunter class + skills (Blink/Mark/Void Arrow Rain) | 45 min |
| 6 | Equipment slot + weapon pickup (equip/absorb) | 30 min |
| 7 | Loot table + enemy loot drops | 30 min |
| 8 | Elite enemy modifiers | 30 min |
| 9 | Room system — room generator + wave spawning | 45 min |
| 10 | Boss — Bug Queen (3 phases) | 45 min |
| 11 | Meta progression — glimmer + permanent upgrades | 20 min |
| 12 | Collection codex | 20 min |
| 13 | UI overhaul — HUD + settlement + lobby | 45 min |
| 14 | Integration — wire everything together | 15 min |
| **Total** | | **~7 hours** |

Each task produces working, testable code. Tasks 1-6 are the minimum playable prototype (shoot enemies, pick up weapons). Tasks 7-10 add the roguelike structure. Tasks 11-14 add progression and polish.
