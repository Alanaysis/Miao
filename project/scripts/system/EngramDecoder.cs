using Godot;
using System.Collections.Generic;
using Miao.Pickup;
using Miao.Weapon;

namespace Miao.System;

/// <summary>
/// 记忆水晶数据 — 存储水晶的武器数据和稀有度
/// </summary>
public class EngramData
{
    public WeaponData WeaponData { get; }
    public Rarity Rarity { get; }

    public EngramData(WeaponData weaponData, Rarity rarity)
    {
        WeaponData = weaponData;
        Rarity = rarity;
    }
}

/// <summary>
/// 记忆水晶解码器 — 管理收集的水晶和解码逻辑
/// 结算时由 DecodeUI 调用，将水晶解码为武器
/// </summary>
public partial class EngramDecoder : Node
{
    public List<EngramData> CollectedEngrams { get; private set; } = new();
    public List<WeaponData> DecodedWeapons { get; private set; } = new();

    /// <summary>
    /// 添加一个记忆水晶到收集列表（提取数据存储，避免引用已释放的节点）
    /// </summary>
    public void AddEngram(MemoryEngram engram)
    {
        var data = new EngramData(engram.GetWeaponData(), engram.GetRarity());
        CollectedEngrams.Add(data);
    }

    /// <summary>
    /// 解码所有未解码的水晶
    /// </summary>
    public void DecodeAll()
    {
        foreach (var engram in CollectedEngrams)
        {
            if (engram.WeaponData != null && !DecodedWeapons.Contains(engram.WeaponData))
            {
                DecodedWeapons.Add(engram.WeaponData);
            }
        }
    }

    /// <summary>
    /// 解码指定索引的单个水晶
    /// </summary>
    public WeaponData DecodeSingle(int index)
    {
        if (index < 0 || index >= CollectedEngrams.Count) return null;
        var weapon = CollectedEngrams[index].WeaponData;
        if (weapon != null && !DecodedWeapons.Contains(weapon))
        {
            DecodedWeapons.Add(weapon);
        }
        return weapon;
    }

    /// <summary>
    /// 获取未解码的水晶数量
    /// </summary>
    public int GetUndecodedCount()
    {
        int count = 0;
        foreach (var engram in CollectedEngrams)
        {
            if (engram.WeaponData != null && !DecodedWeapons.Contains(engram.WeaponData))
            {
                count++;
            }
        }
        return count;
    }

    /// <summary>
    /// 清空所有水晶和解码结果
    /// </summary>
    public void Clear()
    {
        CollectedEngrams.Clear();
        DecodedWeapons.Clear();
    }
}
