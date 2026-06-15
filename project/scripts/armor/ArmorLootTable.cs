using Godot;
using System.Collections.Generic;
using System.Linq;
using Miao.Data;
using Miao.Weapon;

namespace Miao.Armor;

public static class ArmorLootTable
{
    private static List<ArmorData> _allArmor;

    private static void EnsureLoaded()
    {
        if (_allArmor != null) return;

        var wrapper = DataLoader.Load<ArmorWrapper>("armor.json");
        _allArmor = wrapper?.ArmorSets?.ToList() ?? new List<ArmorData>();
    }

    /// <summary>
    /// 获取所有通用护甲（无职业限制），按稀有度筛选
    /// </summary>
    public static List<ArmorData> GetDropPool(Rarity rarity, string classId = null)
    {
        EnsureLoaded();
        string rarityStr = rarity.ToString().ToLower();

        return _allArmor.Where(a =>
        {
            if (a.Rarity != rarityStr) return false;
            // 通用护甲或匹配当前职业
            if (string.IsNullOrEmpty(a.ClassRestriction)) return true;
            return a.ClassRestriction == classId;
        }).ToList();
    }

    /// <summary>
    /// 随机生成一个护甲掉落
    /// </summary>
    public static ArmorData GenerateArmorDrop(int roomIndex, string classId)
    {
        var rarity = RollArmorRarity(roomIndex);
        var pool = GetDropPool(rarity, classId);

        if (pool.Count == 0)
        {
            // fallback: 降一级稀有度
            if (rarity > Rarity.Common)
                return GenerateArmorDrop(roomIndex - 1, classId);
            // 再 fallback: 通用 common
            pool = GetDropPool(Rarity.Common);
        }

        if (pool.Count == 0) return null;
        return pool[GD.RandRange(0, pool.Count - 1)];
    }

    private static Rarity RollArmorRarity(int roomIndex)
    {
        return roomIndex switch
        {
            0 => GD.Randf() < 0.7f ? Rarity.Common : Rarity.Uncommon,
            1 => RollWeighted(new[] { (Rarity.Common, 40), (Rarity.Uncommon, 45), (Rarity.Rare, 15) }),
            2 => RollWeighted(new[] { (Rarity.Uncommon, 30), (Rarity.Rare, 50), (Rarity.Epic, 20) }),
            3 => RollWeighted(new[] { (Rarity.Rare, 40), (Rarity.Epic, 45), (Rarity.Legendary, 15) }),
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
    /// 获取指定职业的初始护甲套装
    /// </summary>
    public static List<ArmorData> GetStarterSet(string classId)
    {
        EnsureLoaded();
        // 初始套装 = 所有 starter_ 开头的通用护甲
        return _allArmor.Where(a => a.Id.StartsWith("starter_")).ToList();
    }

    private class ArmorWrapper
    {
        public List<ArmorData> ArmorSets { get; set; }
    }
}
