using System.Collections.Generic;
using System.Linq;
using Miao.Data;

namespace Miao.Armor;

/// <summary>
/// Mod 数据加载器 — 从 mods.json 加载并缓存 Mod 数据
/// </summary>
public static class ModLoader
{
    private static Dictionary<string, ModData> _modCache;

    private static void EnsureLoaded()
    {
        if (_modCache != null) return;

        var wrapper = DataLoader.Load<ModsWrapper>("mods.json");
        _modCache = new Dictionary<string, ModData>();

        if (wrapper?.Mods != null)
        {
            foreach (var mod in wrapper.Mods)
            {
                _modCache[mod.Id] = mod;
            }
        }
    }

    /// <summary>
    /// 根据 ID 获取 Mod 数据
    /// </summary>
    public static ModData GetMod(string modId)
    {
        EnsureLoaded();
        _modCache.TryGetValue(modId, out var mod);
        return mod;
    }

    /// <summary>
    /// 根据 ID 列表获取所有 Mod 数据
    /// </summary>
    public static List<ModData> GetMods(IEnumerable<string> modIds)
    {
        EnsureLoaded();
        var result = new List<ModData>();
        foreach (var id in modIds)
        {
            if (_modCache.TryGetValue(id, out var mod))
            {
                result.Add(mod);
            }
        }
        return result;
    }

    private class ModsWrapper
    {
        public List<ModData> Mods { get; set; }
    }
}
