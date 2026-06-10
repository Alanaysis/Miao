using Godot;
using Miao.Armor;
using Miao.Weapon;

namespace Miao.System;

public static class LightLevelCalculator
{
    /// <summary>
    /// 计算总光等 = 所有装备光等之平均值
    /// </summary>
    public static int CalculateTotalLightLevel(WeaponData weapon, ArmorManager armorManager)
    {
        int totalLight = 0;
        int itemCount = 0;

        // Weapon light level
        if (weapon != null)
        {
            totalLight += GetWeaponLightLevel(weapon);
            itemCount++;
        }

        // Armor light levels
        if (armorManager != null)
        {
            foreach (var slot in armorManager.Slots.Values)
            {
                if (slot.HasArmor())
                {
                    totalLight += slot.GetLightLevel();
                    itemCount++;
                }
            }
        }

        return itemCount > 0 ? totalLight / itemCount : 0;
    }

    /// <summary>
    /// 根据稀有度获取武器光等
    /// </summary>
    public static int GetWeaponLightLevel(WeaponData weapon)
    {
        // Base light level by rarity
        int baseLight = weapon.Rarity switch
        {
            Rarity.Common => 10,
            Rarity.Uncommon => 15,
            Rarity.Rare => 20,
            Rarity.Epic => 30,
            Rarity.Legendary => 40,
            _ => 10
        };

        // Legendary trait level bonus
        if (weapon.Rarity == Rarity.Legendary && weapon.LegendaryTraitLevel > 1)
        {
            baseLight += weapon.LegendaryTraitLevel * 2;
        }

        return baseLight;
    }

    /// <summary>
    /// 光等对伤害的倍率影响
    /// 光等越高，伤害越高
    /// </summary>
    public static float GetDamageMultiplier(int totalLightLevel, int enemyLightLevel = 0)
    {
        // Base: 1.0x at light level 10
        // +0.02x per light level above 10
        // -0.02x per light level below 10 (min 0.5x)
        float diff = totalLightLevel - 10;
        float mult = 1.0f + diff * 0.02f;
        return Mathf.Clamp(mult, 0.5f, 3.0f);
    }

    /// <summary>
    /// 光等差异对受到伤害的影响
    /// 光等越低，受到伤害越高
    /// </summary>
    public static float GetIncomingDamageMultiplier(int totalLightLevel, int enemyLightLevel = 0)
    {
        float diff = enemyLightLevel - totalLightLevel;
        float mult = 1.0f + diff * 0.01f;
        return Mathf.Clamp(mult, 0.5f, 2.0f);
    }
}
