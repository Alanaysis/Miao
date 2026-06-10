using Godot;
using System.Collections.Generic;
using System.Linq;

namespace Miao.Armor;

public partial class ArmorManager : Node
{
    public Dictionary<ArmorSlot, ArmorSlotManager> Slots { get; private set; } = new();

    public override void _Ready()
    {
        // Create all 5 slots
        foreach (ArmorSlot slot in System.Enum.GetValues<ArmorSlot>())
        {
            var slotManager = new ArmorSlotManager(slot);
            Slots[slot] = slotManager;
            AddChild(slotManager);
        }
    }

    public bool EquipArmor(ArmorData armor)
    {
        if (armor == null) return false;

        ArmorSlot slot = ParseSlot(armor.Slot);
        if (Slots.ContainsKey(slot))
        {
            Slots[slot].Equip(armor);
            return true;
        }
        return false;
    }

    public ArmorData UnequipArmor(ArmorSlot slot)
    {
        if (Slots.ContainsKey(slot))
        {
            return Slots[slot].Unequip();
        }
        return null;
    }

    public int GetTotalLightLevel()
    {
        int total = 0;
        int count = 0;
        foreach (var slot in Slots.Values)
        {
            if (slot.HasArmor())
            {
                total += slot.GetLightLevel();
                count++;
            }
        }
        return count > 0 ? total / count : 0;
    }

    public int GetTotalHealthBonus()
    {
        return Slots.Values.Where(s => s.HasArmor()).Sum(s => s.GetStats()?.Health ?? 0);
    }

    public int GetTotalArmorBonus()
    {
        return Slots.Values.Where(s => s.HasArmor()).Sum(s => s.GetStats()?.Armor ?? 0);
    }

    public float GetTotalMoveSpeedBonus()
    {
        return Slots.Values.Where(s => s.HasArmor()).Sum(s => s.GetStats()?.MoveSpeed ?? 0);
    }

    // TODO: Replace with actual ModData type when mod system is implemented
    public List<string> GetAllEquippedMods()
    {
        var mods = new List<string>();
        foreach (var slot in Slots.Values)
        {
            if (slot.EquippedArmor?.EquippedMods != null)
            {
                mods.AddRange(slot.EquippedArmor.EquippedMods);
            }
        }
        return mods;
    }

    private ArmorSlot ParseSlot(string slotStr)
    {
        return slotStr?.ToLower() switch
        {
            "head" => ArmorSlot.Head,
            "chest" => ArmorSlot.Chest,
            "hands" => ArmorSlot.Hands,
            "legs" => ArmorSlot.Legs,
            "class_item" => ArmorSlot.ClassItem,
            _ => ArmorSlot.Head
        };
    }
}
