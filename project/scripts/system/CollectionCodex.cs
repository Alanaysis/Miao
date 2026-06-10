using Godot;
using System;
using System.Collections.Generic;
using Miao.Weapon;

namespace Miao.System;

public partial class CollectionCodex : Node
{
    public static CollectionCodex Instance { get; private set; }

    public HashSet<string> DiscoveredWeapons { get; private set; } = new();
    public HashSet<string> WeaponsClearedWith { get; private set; } = new();
    public HashSet<string> WeaponsPerkClearedWith { get; private set; } = new();
    public HashSet<PerkId> DiscoveredPerks { get; private set; } = new();
    public HashSet<string> DiscoveredEnemies { get; private set; } = new();
    public int TotalDiscoveries => DiscoveredWeapons.Count + DiscoveredPerks.Count + DiscoveredEnemies.Count;

    // 每解锁 10 个条目，+2% 全局伤害
    public float CollectionDamageBonus => 1.0f + (TotalDiscoveries / 10) * 0.02f;

    public override void _Ready()
    {
        Instance = this;
        Load();
    }

    public void RegisterWeapon(WeaponData data)
    {
        string key = $"{data.Type}_{data.Rarity}";
        if (DiscoveredWeapons.Add(key))
        {
            GD.Print($"图鉴解锁：{data.DisplayName}");

            // Also unlock in weapon pool if epic/legendary
            if (data.Rarity >= Rarity.Epic && WeaponUnlockPool.Instance != null)
            {
                WeaponUnlockPool.Instance.UnlockWeapon(data.Type, data.Rarity);
            }
        }

        foreach (var perk in data.Perks)
        {
            if (DiscoveredPerks.Add(perk))
            {
                GD.Print($"图鉴解锁 Perk：{PerkSystem.PerkInfo[perk].Name}");
            }
        }

        Save();
    }

    public void RegisterClearWeapon(WeaponData data)
    {
        string key = $"{data.Type}_{data.Rarity}";
        WeaponsClearedWith.Add(key);

        if (data.Perks.Count > 0)
        {
            WeaponsPerkClearedWith.Add(key);
        }

        Save();
    }

    public void RegisterEnemy(string enemyId)
    {
        if (DiscoveredEnemies.Add(enemyId))
        {
            GD.Print($"图鉴解锁敌人：{enemyId}");
            Save();
        }
    }

    public void Save()
    {
        var config = new ConfigFile();

        var weaponList = new string[DiscoveredWeapons.Count];
        DiscoveredWeapons.CopyTo(weaponList);
        config.SetValue("codex", "weapons", string.Join(",", weaponList));

        var clearedList = new string[WeaponsClearedWith.Count];
        WeaponsClearedWith.CopyTo(clearedList);
        config.SetValue("codex", "cleared_with", string.Join(",", clearedList));

        var perkList = new string[DiscoveredPerks.Count];
        int i = 0;
        foreach (var p in DiscoveredPerks) perkList[i++] = p.ToString();
        config.SetValue("codex", "perks", string.Join(",", perkList));

        var enemyList = new string[DiscoveredEnemies.Count];
        DiscoveredEnemies.CopyTo(enemyList);
        config.SetValue("codex", "enemies", string.Join(",", enemyList));

        config.Save("user://collection_codex.cfg");
    }

    public void Load()
    {
        var config = new ConfigFile();
        if (config.Load("user://collection_codex.cfg") != Error.Ok) return;

        var weapons = config.GetValue("codex", "weapons", "").ToString();
        if (!string.IsNullOrEmpty(weapons))
        {
            foreach (var w in weapons.Split(','))
                DiscoveredWeapons.Add(w);
        }

        var cleared = config.GetValue("codex", "cleared_with", "").ToString();
        if (!string.IsNullOrEmpty(cleared))
        {
            foreach (var w in cleared.Split(','))
                WeaponsClearedWith.Add(w);
        }

        var perks = config.GetValue("codex", "perks", "").ToString();
        if (!string.IsNullOrEmpty(perks))
        {
            foreach (var p in perks.Split(','))
            {
                if (Enum.TryParse<PerkId>(p, out var perkId))
                    DiscoveredPerks.Add(perkId);
            }
        }

        var enemies = config.GetValue("codex", "enemies", "").ToString();
        if (!string.IsNullOrEmpty(enemies))
        {
            foreach (var e in enemies.Split(','))
                DiscoveredEnemies.Add(e);
        }
    }
}
