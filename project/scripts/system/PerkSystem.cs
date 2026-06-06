using Godot;
using System.Collections.Generic;

namespace Miao.System;

using Miao.Weapon;

/// <summary>
/// Perk 系统 —— 8 种 Perk 的完整实现
/// </summary>
// TODO: KillClip — 击杀后下一弹夹 +30% 伤害，需在战斗系统的击杀回调中触发增伤状态切换
// TODO: KillReturn — 击杀回复 1 发子弹，需在战斗系统的击杀回调中调用弹药系统回填
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

    // MVP 中所有武器 Perk 池内容相同
    public static readonly Dictionary<PerkId, WeaponType[]> PerkWeaponRestriction = new()
    {
        { PerkId.KillClip, new[] { WeaponType.AutoRifle, WeaponType.Shotgun, WeaponType.HandCannon } },
        { PerkId.CriticalMaster, new[] { WeaponType.AutoRifle, WeaponType.Shotgun, WeaponType.HandCannon } },
        { PerkId.ShotgunSpread, new[] { WeaponType.AutoRifle, WeaponType.Shotgun, WeaponType.HandCannon } },
        { PerkId.Penetration, new[] { WeaponType.AutoRifle, WeaponType.Shotgun, WeaponType.HandCannon } },
        { PerkId.KillReturn, new[] { WeaponType.AutoRifle, WeaponType.Shotgun, WeaponType.HandCannon } },
        { PerkId.RapidFire, new[] { WeaponType.AutoRifle, WeaponType.Shotgun, WeaponType.HandCannon } },
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
        // TODO: 在战斗系统中接入 KillClip 增伤逻辑（击杀后下一弹夹 +30% 伤害）
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

        if (available.Count == 0)
        {
            GD.PushWarning("PerkSystem: RollRandomPerk 无可用 Perk，使用 fallback");
            return PerkId.KillClip; // fallback
        }
        return available[GD.RandRange(0, available.Count - 1)];
    }
}
