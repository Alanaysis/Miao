using System.Text.Json.Serialization;

namespace Miao.Player;

/// <summary>
/// 星相（Aspect）定义数据
/// 每个星相为子职业定义一种玩法变体，并提供碎片槽位
/// </summary>
public class AspectConfig
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("fragments_slots")]
    public int FragmentsSlots { get; set; } = 2;
}
