using Godot;

namespace Miao.Armor;

public partial class ArmorSlotManager : Node
{
    public ArmorSlot SlotType { get; private set; }
    public ArmorData EquippedArmor { get; private set; }

    public ArmorSlotManager(ArmorSlot slotType)
    {
        SlotType = slotType;
    }

    public void Equip(ArmorData armor)
    {
        if (armor == null) return;

        // Verify slot type matches
        if (armor.Slot.ToLower() != SlotType.ToString().ToLower())
        {
            GD.PushError($"ArmorSlotManager: 试图在 {SlotType} 槽装备 {armor.Slot} 类型的护甲");
            return;
        }

        EquippedArmor = armor;
        GD.Print($"装备护甲: {armor.Name} ({SlotType})");
    }

    public ArmorData Unequip()
    {
        var old = EquippedArmor;
        EquippedArmor = null;
        return old;
    }

    public bool HasArmor()
    {
        return EquippedArmor != null;
    }

    public int GetLightLevel()
    {
        return EquippedArmor?.LightLevel ?? 0;
    }

    public ArmorStats GetStats()
    {
        return EquippedArmor?.Stats;
    }

    public int GetTotalModSlots()
    {
        return EquippedArmor?.ModSlots ?? 0;
    }
}
