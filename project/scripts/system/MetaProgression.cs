using Godot;
using System.Collections.Generic;

namespace Miao.System;

public partial class MetaProgression : Node
{
    public static MetaProgression Instance { get; private set; }

    // 持久化数据
    public int Glimmer { get; private set; }

    // Shop unlock tracking
    private HashSet<string> _unlockedSubclasses = new();
    private HashSet<string> _unlockedMods = new();
    private HashSet<string> _unlockedAspects = new();
    private int _lightModuleLevel;

    public override void _Ready()
    {
        Instance = this;
        Load();
    }

    // ==================== Subclass ====================

    public bool IsSubclassUnlocked(string className, string element)
    {
        return _unlockedSubclasses.Contains($"{className}_{element}");
    }

    public bool TryUnlockSubclass(string className, string element, int cost)
    {
        var key = $"{className}_{element}";
        if (_unlockedSubclasses.Contains(key)) return false;
        if (Glimmer < cost) return false;

        Glimmer -= cost;
        _unlockedSubclasses.Add(key);
        Save();
        return true;
    }

    // ==================== Mod ====================

    public bool IsModUnlocked(string modId)
    {
        return _unlockedMods.Contains(modId);
    }

    public bool TryUnlockMod(string modId, int cost)
    {
        if (_unlockedMods.Contains(modId)) return false;
        if (Glimmer < cost) return false;

        Glimmer -= cost;
        _unlockedMods.Add(modId);
        Save();
        return true;
    }

    // ==================== Aspect ====================

    public bool IsAspectUnlocked(string aspectId)
    {
        return _unlockedAspects.Contains(aspectId);
    }

    public bool TryUnlockAspect(string aspectId, int cost)
    {
        if (_unlockedAspects.Contains(aspectId)) return false;
        if (Glimmer < cost) return false;

        Glimmer -= cost;
        _unlockedAspects.Add(aspectId);
        Save();
        return true;
    }

    // ==================== Light Module ====================

    public int GetLightModuleLevel()
    {
        return _lightModuleLevel;
    }

    public bool TryBuyLightModule(int cost)
    {
        if (Glimmer < cost) return false;

        Glimmer -= cost;
        _lightModuleLevel++;
        Save();
        return true;
    }

    // ==================== Stat Bonuses (meta upgrades) ====================

    public int GetBonusHealth()
    {
        return 0; // TODO: implement based on purchased upgrades
    }

    public float GetBonusMoveSpeed()
    {
        return 1.0f; // TODO: implement based on purchased upgrades
    }

    public float GetBonusDamage()
    {
        return 1.0f; // TODO: implement based on purchased upgrades
    }

    public float GetBonusDropRate()
    {
        return 1.0f; // TODO: implement based on purchased upgrades
    }

    // ==================== Glimmer ====================

    public void AddGlimmer(int amount)
    {
        Glimmer += amount;
        Save();
    }

    // ==================== Save / Load ====================

    public void Save()
    {
        var config = new ConfigFile();
        config.SetValue("meta", "glimmer", Glimmer);
        config.SetValue("meta", "light_module_level", _lightModuleLevel);

        // Save unlocked subclasses
        var subclassArr = new string[_unlockedSubclasses.Count];
        _unlockedSubclasses.CopyTo(subclassArr);
        config.SetValue("meta", "unlocked_subclasses", string.Join(",", subclassArr));

        // Save unlocked mods
        var modArr = new string[_unlockedMods.Count];
        _unlockedMods.CopyTo(modArr);
        config.SetValue("meta", "unlocked_mods", string.Join(",", modArr));

        // Save unlocked aspects
        var aspectArr = new string[_unlockedAspects.Count];
        _unlockedAspects.CopyTo(aspectArr);
        config.SetValue("meta", "unlocked_aspects", string.Join(",", aspectArr));

        config.Save("user://meta_progress.cfg");
    }

    public void Load()
    {
        var config = new ConfigFile();
        if (config.Load("user://meta_progress.cfg") != Error.Ok) return;

        Glimmer = (int)config.GetValue("meta", "glimmer", 0);
        _lightModuleLevel = (int)config.GetValue("meta", "light_module_level", 0);

        // Load unlocked subclasses
        var subclassStr = (string)config.GetValue("meta", "unlocked_subclasses", "");
        _unlockedSubclasses.Clear();
        if (!string.IsNullOrEmpty(subclassStr))
        {
            foreach (var key in subclassStr.Split(','))
            {
                if (!string.IsNullOrWhiteSpace(key))
                    _unlockedSubclasses.Add(key.Trim());
            }
        }

        // Load unlocked mods
        var modStr = (string)config.GetValue("meta", "unlocked_mods", "");
        _unlockedMods.Clear();
        if (!string.IsNullOrEmpty(modStr))
        {
            foreach (var key in modStr.Split(','))
            {
                if (!string.IsNullOrWhiteSpace(key))
                    _unlockedMods.Add(key.Trim());
            }
        }

        // Load unlocked aspects
        var aspectStr = (string)config.GetValue("meta", "unlocked_aspects", "");
        _unlockedAspects.Clear();
        if (!string.IsNullOrEmpty(aspectStr))
        {
            foreach (var key in aspectStr.Split(','))
            {
                if (!string.IsNullOrWhiteSpace(key))
                    _unlockedAspects.Add(key.Trim());
            }
        }
    }
}
