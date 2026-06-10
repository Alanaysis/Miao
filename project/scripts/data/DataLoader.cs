using Godot;
using System.Collections.Generic;
using System.Text.Json;

namespace Miao.Data;

public static class DataLoader
{
    private static Dictionary<string, JsonElement> _cache = new();

    public static T Load<T>(string filename)
    {
        if (_cache.TryGetValue(filename, out var cached))
        {
            return cached.Deserialize<T>();
        }

        var path = $"res://data/{filename}";
        if (!FileAccess.FileExists(path))
        {
            GD.PushError($"DataLoader: 文件不存在 {path}");
            return default;
        }

        var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        var json = file.GetAsText();
        file.Close();

        var doc = JsonDocument.Parse(json);
        _cache[filename] = doc.RootElement.Clone();
        return doc.RootElement.Deserialize<T>();
    }

    public static void ClearCache()
    {
        _cache.Clear();
    }
}
