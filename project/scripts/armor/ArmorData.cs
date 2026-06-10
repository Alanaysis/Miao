using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Miao.Armor;

public enum ArmorSlot
{
    Head,       // 头盔
    Chest,      // 胸甲
    Hands,      // 手套
    Legs,       // 腿甲
    ClassItem   // 职业物品
}

public class ArmorData
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("slot")]
    public string Slot { get; set; }

    [JsonPropertyName("light_level")]
    public int LightLevel { get; set; }

    [JsonPropertyName("rarity")]
    public string Rarity { get; set; }

    [JsonPropertyName("stats")]
    public ArmorStats Stats { get; set; }

    [JsonPropertyName("mod_slots")]
    public int ModSlots { get; set; } = 1;

    [JsonPropertyName("class_restriction")]
    public string ClassRestriction { get; set; } // "hunter", "titan", "warlock", or null for all

    // Runtime mod slots
    // TODO: Replace with actual ModData type when mod system is implemented
    public List<string> EquippedMods { get; set; } = new();
}

public class ArmorStats
{
    [JsonPropertyName("health")]
    public int Health { get; set; }

    [JsonPropertyName("armor")]
    public int Armor { get; set; }

    [JsonPropertyName("move_speed")]
    public float MoveSpeed { get; set; }
}
