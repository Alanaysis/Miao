using Godot;
using global::System;
using global::System.Collections.Generic;
using global::System.Linq;
using Miao.System;

namespace Miao.Weapon;

public static class LootTable
{
    /// <summary>
    /// 掉落武器的概率（30% 感觉偏高，待定）
    /// </summary>
    public const float BaseDropChance = 0.3f; // TODO: 调整平衡

    public static Rarity RollRarity(int roomIndex, bool isBoss)
    {
        if (isBoss)
        {
            float roll = GD.Randf();
            if (roll < 0.10f) return Rarity.Legendary;
            if (roll < 0.40f) return Rarity.Epic;
            if (roll < 0.80f) return Rarity.Rare;
            return Rarity.Uncommon;
        }

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

    public static WeaponType RollWeaponType()
    {
        var values = Enum.GetValues<WeaponType>();
        return values[GD.RandRange(0, values.Length - 1)];
    }

    private static WeaponType RollWeaponTypeWithUnlockCheck(Rarity rarity)
    {
        // Get unlocked weapon types for this rarity
        var unlockedTypes = GetUnlockedWeaponTypes(rarity);

        if (unlockedTypes.Count > 0)
        {
            // Pick random from unlocked types
            return unlockedTypes[GD.RandRange(0, unlockedTypes.Count - 1)];
        }

        // If no weapons unlocked for this rarity, fallback to lower rarity
        if (rarity > Rarity.Common)
        {
            GD.Print($"No weapons unlocked for {rarity}, falling back to {rarity - 1}");
            return RollWeaponTypeWithUnlockCheck(rarity - 1);
        }

        // Should never reach here if defaults are loaded properly
        GD.PushWarning("No unlocked weapons found, using random type");
        return RollWeaponType();
    }

    public static List<WeaponType> GetUnlockedWeaponTypes(Rarity rarity)
    {
        if (WeaponUnlockPool.Instance != null)
        {
            return WeaponUnlockPool.Instance.GetUnlockedTypesForRarity(rarity);
        }
        // Fallback: return all types if unlock pool not initialized
        return Enum.GetValues<WeaponType>().ToList();
    }

    public static WeaponData GenerateWeapon(int roomIndex, bool isBoss)
    {
        var rarity = RollRarity(roomIndex, isBoss);

        // 应用 MetaProgression 掉率加成：有概率提升一个稀有度
        if (MetaProgression.Instance != null && rarity < Rarity.Legendary)
        {
            float dropBonus = MetaProgression.Instance.GetBonusDropRate();
            if (GD.Randf() < (dropBonus - 1.0f))
            {
                rarity = rarity + 1; // 提升一级稀有度
            }
        }

        // Check if weapon type+rarity is unlocked, fallback to lower rarity if not
        var type = RollWeaponTypeWithUnlockCheck(rarity);

        var data = new WeaponData
        {
            Type = type,
            Rarity = rarity,
            DisplayName = GenerateName(type, rarity),
            Perks = new List<PerkId>()
        };

        switch (type)
        {
            case WeaponType.AutoRifle:
                data.BaseDamage = 8;
                data.FireRate = 6.0f;
                data.BulletSpeed = 500;
                data.KnockbackForce = 50;
                break;
            case WeaponType.PulseRifle:
                data.BaseDamage = 12;
                data.FireRate = 2.0f;
                data.BulletSpeed = 500;
                data.KnockbackForce = 40;
                break;
            case WeaponType.ScoutRifle:
                data.BaseDamage = 20;
                data.FireRate = 2.5f;
                data.BulletSpeed = 600;
                data.KnockbackForce = 60;
                break;
            case WeaponType.HandCannon:
                data.BaseDamage = 25;
                data.FireRate = 2.5f;
                data.BulletSpeed = 600;
                data.KnockbackForce = 100;
                break;
            case WeaponType.SMG:
                data.BaseDamage = 5;
                data.FireRate = 10.0f;
                data.BulletSpeed = 450;
                data.SpreadAngle = 0.15f;
                data.KnockbackForce = 20;
                break;
            case WeaponType.Shotgun:
                data.BaseDamage = 15;
                data.FireRate = 1.5f;
                data.BulletSpeed = 400;
                data.BulletCount = 6;
                data.SpreadAngle = Mathf.DegToRad(30);
                data.KnockbackForce = 150;
                break;
            case WeaponType.SniperRifle:
                data.BaseDamage = 50;
                data.FireRate = 0.8f;
                data.BulletSpeed = 800;
                data.KnockbackForce = 80;
                break;
            case WeaponType.FusionRifle:
                data.BaseDamage = 30;
                data.FireRate = 1.0f;
                data.BulletSpeed = 500;
                data.SpreadAngle = 0.2f;
                data.KnockbackForce = 60;
                break;
            case WeaponType.RocketLauncher:
                data.BaseDamage = 80;
                data.FireRate = 0.5f;
                data.BulletSpeed = 300;
                data.KnockbackForce = 200;
                break;
            case WeaponType.Sword:
                data.BaseDamage = 20;
                data.FireRate = 4.0f;
                data.BulletSpeed = 0;
                data.KnockbackForce = 50;
                break;
        }

        float rarityMult = rarity switch
        {
            Rarity.Uncommon => 1.1f,
            Rarity.Rare => 1.25f,
            Rarity.Epic => 1.4f,
            Rarity.Legendary => 1.6f,
            _ => 1.0f
        };
        data.BaseDamage = Mathf.RoundToInt(data.BaseDamage * rarityMult);

        int perkCount = data.MaxPerkSlots;
        var exclude = new List<PerkId>();
        for (int i = 0; i < perkCount; i++)
        {
            var perk = PerkSystem.RollRandomPerk(type, exclude);
            data.Perks.Add(perk);
            exclude.Add(perk);
        }

        // 金武：分配独有特性
        if (rarity == Rarity.Legendary)
        {
            var traits = Enum.GetValues<LegendaryTraitId>();
            data.LegendaryTrait = traits[GD.RandRange(1, traits.Length - 1)]; // skip Default
            data.LegendaryTraitLevel = 1;
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
            WeaponType.PulseRifle => "战斗步枪",
            WeaponType.ScoutRifle => "斥候步枪",
            WeaponType.HandCannon => "手炮",
            WeaponType.SMG => "冲锋枪",
            WeaponType.Shotgun => "霰弹枪",
            WeaponType.SniperRifle => "狙击步枪",
            WeaponType.FusionRifle => "融合步枪",
            WeaponType.RocketLauncher => "火箭筒",
            WeaponType.Sword => "刀剑",
            _ => "武器"
        };

        return $"{prefix}{weapon}";
    }
}
