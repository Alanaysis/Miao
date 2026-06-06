using Godot;
using System.Collections.Generic;

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
        return (WeaponType)GD.RandRange(0, 2);
    }

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
            data.LegendaryTrait = LegendaryTraitId.Default;
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
            WeaponType.Shotgun => "霰弹枪",
            WeaponType.HandCannon => "手炮",
            _ => "武器"
        };

        return $"{prefix}{weapon}";
    }
}
