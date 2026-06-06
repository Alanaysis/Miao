using Godot;
using System.Collections.Generic;
using Miao.System;

namespace Miao.Weapon;

public partial class EquipmentSlot : Node2D
{
    public Weapon CurrentWeapon { get; private set; }
    [Export] public PackedScene WeaponScene;

    public void EquipWeapon(WeaponData data)
    {
        if (CurrentWeapon != null)
        {
            CurrentWeapon.QueueFree();
        }

        CurrentWeapon = WeaponScene.Instantiate<Weapon>();
        CurrentWeapon.SetWeaponData(data);
        AddChild(CurrentWeapon);

        CollectionCodex.Instance?.RegisterWeapon(data);
    }

    /// <summary>
    /// 灌注：蓝武以上才能灌注，必须同类型同稀有度
    /// </summary>
    public bool CanInfuse(WeaponData newWeaponData)
    {
        if (CurrentWeapon?.Data == null) return false;
        if (newWeaponData.Perks.Count == 0) return false;

        var currentData = CurrentWeapon.Data;

        // 白武、绿武不显示灌注选项
        if (newWeaponData.Rarity < Rarity.Rare) return false;
        // 必须同类型
        if (newWeaponData.Type != currentData.Type) return false;
        // 必须同稀有度
        if (newWeaponData.Rarity != currentData.Rarity) return false;

        return true;
    }

    /// <summary>
    /// 灌注：选择新武器的 1 个 Perk 替换当前武器的 1 个 Perk
    /// </summary>
    public bool TryInfusePerk(WeaponData newWeaponData, PerkId sourcePerk, int targetSlotIndex)
    {
        if (!CanInfuse(newWeaponData)) return false;
        if (!newWeaponData.Perks.Contains(sourcePerk)) return false;

        var currentData = CurrentWeapon.Data;
        if (targetSlotIndex < 0 || targetSlotIndex >= currentData.Perks.Count) return false;

        currentData.Perks[targetSlotIndex] = sourcePerk;
        return true;
    }

    public List<PerkId> GetCurrentPerks()
    {
        return CurrentWeapon?.Data?.Perks ?? new List<PerkId>();
    }
}
