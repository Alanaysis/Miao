using Godot;

namespace Miao.System;

public partial class MetaProgression : Node
{
    public static MetaProgression Instance { get; private set; }

    // 持久化数据
    public int Glimmer { get; private set; }
    public int BaseHealthLevel { get; private set; }
    public int BaseDamageLevel { get; private set; }
    public int MoveSpeedLevel { get; private set; }
    public int DropRateLevel { get; private set; }
    public int StartWeaponLevel { get; private set; }

    private readonly int[] _upgradeCosts = { 100, 200, 400, 800, 1600, 3200, 6400, 12800, 25600, 51200 };

    public override void _Ready()
    {
        Instance = this;
        Load();
    }

    public int GetBonusHealth() => BaseHealthLevel * 10;
    public float GetBonusDamage() => 1.0f + BaseDamageLevel * 0.05f;
    public float GetBonusMoveSpeed() => 1.0f + MoveSpeedLevel * 0.03f;
    public float GetBonusDropRate() => 1.0f + DropRateLevel * 0.05f;

    public bool CanAffordUpgrade(int currentLevel, int maxLevel)
    {
        if (currentLevel >= maxLevel) return false;
        return Glimmer >= _upgradeCosts[currentLevel];
    }

    public bool TryUpgrade(string upgradeId, int maxLevel)
    {
        int currentLevel = upgradeId switch
        {
            "base_health" => BaseHealthLevel,
            "base_damage" => BaseDamageLevel,
            "move_speed" => MoveSpeedLevel,
            "drop_rate" => DropRateLevel,
            "start_weapon" => StartWeaponLevel,
            _ => 0
        };

        if (!CanAffordUpgrade(currentLevel, maxLevel)) return false;

        int cost = _upgradeCosts[currentLevel];
        Glimmer -= cost;

        switch (upgradeId)
        {
            case "base_health": BaseHealthLevel++; break;
            case "base_damage": BaseDamageLevel++; break;
            case "move_speed": MoveSpeedLevel++; break;
            case "drop_rate": DropRateLevel++; break;
            case "start_weapon": StartWeaponLevel++; break;
        }

        Save();
        return true;
    }

    public void AddGlimmer(int amount)
    {
        Glimmer += amount;
        Save();
    }

    public void Save()
    {
        var config = new ConfigFile();
        config.SetValue("meta", "glimmer", Glimmer);
        config.SetValue("meta", "base_health_level", BaseHealthLevel);
        config.SetValue("meta", "base_damage_level", BaseDamageLevel);
        config.SetValue("meta", "move_speed_level", MoveSpeedLevel);
        config.SetValue("meta", "drop_rate_level", DropRateLevel);
        config.SetValue("meta", "start_weapon_level", StartWeaponLevel);
        config.Save("user://meta_progress.cfg");
    }

    public void Load()
    {
        var config = new ConfigFile();
        if (config.Load("user://meta_progress.cfg") != Error.Ok) return;

        Glimmer = (int)config.GetValue("meta", "glimmer", 0);
        BaseHealthLevel = (int)config.GetValue("meta", "base_health_level", 0);
        BaseDamageLevel = (int)config.GetValue("meta", "base_damage_level", 0);
        MoveSpeedLevel = (int)config.GetValue("meta", "move_speed_level", 0);
        DropRateLevel = (int)config.GetValue("meta", "drop_rate_level", 0);
        StartWeaponLevel = (int)config.GetValue("meta", "start_weapon_level", 0);
    }
}
