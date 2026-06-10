using Godot;
using System.Collections.Generic;
using System.Linq;
using Miao.Weapon;

namespace Miao.System;

public partial class WeaponUnlockPool : Node
{
    public static WeaponUnlockPool Instance { get; private set; }

    // Unlocked weapon keys: "weapon_type_rarity" (e.g., "auto_rifle_epic")
    private HashSet<string> _unlockedWeapons = new();

    // Map-specific weapon pools: map_id -> list of weapon keys
    private Dictionary<string, List<string>> _mapWeaponPools = new();

    public override void _Ready()
    {
        Instance = this;
        LoadDefaultUnlocks();
        LoadMapPools();
        Load();
    }

    private void LoadDefaultUnlocks()
    {
        // White/green/blue weapons are always unlocked
        var allTypes = System.Enum.GetValues<WeaponType>();
        foreach (var type in allTypes)
        {
            _unlockedWeapons.Add($"{type.ToString().ToLower()}_common");
            _unlockedWeapons.Add($"{type.ToString().ToLower()}_uncommon");
            _unlockedWeapons.Add($"{type.ToString().ToLower()}_rare");
        }
    }

    private void LoadMapPools()
    {
        // Load from maps.json
        var mapsData = Miao.Data.DataLoader.Load<MapsConfig>("maps.json");
        if (mapsData?.Maps != null)
        {
            foreach (var map in mapsData.Maps)
            {
                var pool = new List<string>();
                foreach (var weaponType in map.WeaponPool)
                {
                    pool.Add($"{weaponType}_epic");
                    pool.Add($"{weaponType}_legendary");
                }
                _mapWeaponPools[map.Id] = pool;
            }
        }
    }

    public bool IsWeaponUnlocked(WeaponType type, Rarity rarity)
    {
        string key = $"{type.ToString().ToLower()}_{rarity.ToString().ToLower()}";
        return _unlockedWeapons.Contains(key);
    }

    public void UnlockWeapon(WeaponType type, Rarity rarity)
    {
        string key = $"{type.ToString().ToLower()}_{rarity.ToString().ToLower()}";
        if (_unlockedWeapons.Add(key))
        {
            GD.Print($"武器解锁: {type} {rarity}");
            Save();
        }
    }

    public void UnlockWeaponsFromMap(string mapId)
    {
        if (_mapWeaponPools.ContainsKey(mapId))
        {
            foreach (var weaponKey in _mapWeaponPools[mapId])
            {
                _unlockedWeapons.Add(weaponKey);
            }
            GD.Print($"地图 {mapId} 武器已解锁");
            Save();
        }
    }

    public List<WeaponType> GetUnlockedTypesForRarity(Rarity rarity)
    {
        var result = new List<WeaponType>();
        foreach (var type in System.Enum.GetValues<WeaponType>())
        {
            if (IsWeaponUnlocked(type, rarity))
            {
                result.Add(type);
            }
        }
        return result;
    }

    public void Save()
    {
        var config = new ConfigFile();
        var keys = new string[_unlockedWeapons.Count];
        _unlockedWeapons.CopyTo(keys);
        config.SetValue("weapons", "unlocked", string.Join(",", keys));
        config.Save("user://weapon_unlocks.cfg");
    }

    public void Load()
    {
        var config = new ConfigFile();
        if (config.Load("user://weapon_unlocks.cfg") != Error.Ok) return;

        var data = config.GetValue("weapons", "unlocked", "").ToString();
        if (!string.IsNullOrEmpty(data))
        {
            foreach (var key in data.Split(','))
            {
                _unlockedWeapons.Add(key);
            }
        }
    }
}

// JSON deserialization classes
public class MapsConfig
{
    [System.Text.Json.Serialization.JsonPropertyName("maps")]
    public List<MapConfig> Maps { get; set; }
}

public class MapConfig
{
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public string Id { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string Name { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("theme")]
    public string Theme { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("difficulty")]
    public int Difficulty { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("boss")]
    public string Boss { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("weapon_pool")]
    public List<string> WeaponPool { get; set; }
}
