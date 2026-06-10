using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Miao.Player;

public class SubclassConfig
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("element")]
    public string Element { get; set; }

    [JsonPropertyName("color")]
    public string Color { get; set; }

    [JsonPropertyName("skills")]
    public Dictionary<string, SkillConfig> Skills { get; set; }

    [JsonPropertyName("super")]
    public SuperConfig Super { get; set; }
}

public class SkillConfig
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("cooldown")]
    public float Cooldown { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }
}

public class SuperConfig
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }
}
