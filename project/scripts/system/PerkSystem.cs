namespace Miao.System;

using Miao.Weapon;

/// <summary>
/// Perk 系统（桩实现，完整实现在 Task 4）
/// </summary>
public static class PerkSystem
{
    public static float GetFireRateMultiplier(WeaponData data)
    {
        // TODO: Task 4 - 根据 Perk 列表计算射速倍率
        return 1.0f;
    }

    public static float GetDamageMultiplier(WeaponData data)
    {
        // TODO: Task 4 - 根据 Perk 列表计算伤害倍率
        return 1.0f;
    }

    public static float GetKnockbackMultiplier(WeaponData data)
    {
        // TODO: Task 4 - 根据 Perk 列表计算击退倍率
        return 1.0f;
    }

    public static bool HasPerk(WeaponData data, PerkId perk)
    {
        // TODO: Task 4 - 检查武器是否拥有指定 Perk
        return data.Perks.Contains(perk);
    }
}
