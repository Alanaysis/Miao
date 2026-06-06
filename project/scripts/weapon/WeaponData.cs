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

public enum PerkId
{
    KillClip, CriticalMaster, ShotgunSpread, Penetration,
    KillReturn, RapidFire, StableGrip, HeadHunter
}

public enum LegendaryTraitId
{
    Default, // 占位，后续任务实现更多特性
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
        _ => 0
    };
}
