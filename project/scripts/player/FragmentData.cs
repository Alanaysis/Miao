using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Miao.Player;

public class FragmentConfig
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("rarity")]
    public string Rarity { get; set; }

    [JsonPropertyName("effects")]
    public List<FragmentEffect> Effects { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }
}

public class FragmentEffect
{
    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("stat")]
    public string Stat { get; set; }

    [JsonPropertyName("value")]
    public float Value { get; set; }

    [JsonPropertyName("trigger")]
    public string Trigger { get; set; }

    [JsonPropertyName("effect")]
    public string Effect { get; set; }

    [JsonPropertyName("duration")]
    public float Duration { get; set; }

    [JsonPropertyName("multiplier")]
    public float Multiplier { get; set; }
}
