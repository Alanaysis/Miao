using Godot;
using System.Collections.Generic;

namespace Miao.Weapon;

public enum WeaponType
{
    AutoRifle,      // 自动步枪
    PulseRifle,     // 战斗步枪（3连发）
    ScoutRifle,     // 斥候步枪（慢速高伤精准）
    HandCannon,     // 手炮
    SMG,            // 冲锋枪（极快近距离）
    Shotgun,        // 霰弹枪
    SniperRifle,    // 狙击步枪（极慢极高伤精准）
    FusionRifle,    // 融合步枪（蓄力后发射5发）
    RocketLauncher, // 火箭筒（AOE爆炸）
    Sword           // 刀剑（近战）
}

public enum Rarity
{
    Common,      // 白
    Uncommon,    // 绿
    Rare,        // 蓝
    Epic,        // 紫
    Legendary,   // 金
    Exotic       // 异域（独有特性 + 累计击杀升级）
}

public enum PerkId
{
    KillClip, CriticalMaster, ShotgunSpread, Penetration,
    KillReturn, RapidFire, StableGrip, HeadHunter
}

public enum LegendaryTraitId
{
    Default,
    Firefly,        // 击杀爆炸
    Outlaw,         // 精准击杀大幅加速装填
    Rampage,        // 击杀叠加伤害
    Dragonfly,      // 精准击杀元素爆炸
    KillClip        // 装填后伤害提升
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
    /// 金武独有特性（仅 Legendary 有）
    /// </summary>
    public LegendaryTraitId? LegendaryTrait { get; set; }

    /// <summary>
    /// 金武独有特性的升级等级（通过累计击杀升级）
    /// </summary>
    public int LegendaryTraitLevel { get; set; } = 0;

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
        Rarity.Exotic => 3,
        _ => 0
    };
}
