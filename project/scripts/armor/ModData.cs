using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Miao.Armor;

public enum ModType
{
    WeaponBoost,    // 武器增幅类
    Survival,       // 生存类
    Skill,          // 技能类
    Element,        // 元素增幅类
    Economy,        // 经济类
    Special         // 特殊效果类
}

public class ModData
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("effects")]
    public List<ModEffect> Effects { get; set; }

    [JsonPropertyName("slot_restriction")]
    public string SlotRestriction { get; set; } // null = any slot

    [JsonPropertyName("acquisition")]
    public string Acquisition { get; set; } // "meta_shop" or "drop"

    [JsonPropertyName("cost")]
    public int Cost { get; set; } // Meta shop cost in glimmer
}

public class ModEffect
{
    [JsonPropertyName("stat")]
    public string Stat { get; set; }

    [JsonPropertyName("value")]
    public float Value { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } // "flat" or "percent"
}
