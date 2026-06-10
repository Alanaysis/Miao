using Godot;
using System.Collections.Generic;

namespace Miao.Armor;

public static class ModEffectProcessor
{
    public static float GetStatBonus(List<ModData> mods, string stat)
    {
        float flatBonus = 0;
        float percentBonus = 0;

        foreach (var mod in mods)
        {
            if (mod.Effects == null) continue;
            foreach (var effect in mod.Effects)
            {
                if (effect.Stat == stat)
                {
                    if (effect.Type == "flat")
                        flatBonus += effect.Value;
                    else if (effect.Type == "percent")
                        percentBonus += effect.Value;
                }
            }
        }

        return flatBonus + percentBonus;
    }

    public static float GetFireRateBonus(List<ModData> mods)
    {
        return GetStatBonus(mods, "fire_rate");
    }

    public static float GetDamageBonus(List<ModData> mods)
    {
        return GetStatBonus(mods, "damage");
    }

    public static float GetHealthBonus(List<ModData> mods)
    {
        return GetStatBonus(mods, "health");
    }

    public static float GetArmorBonus(List<ModData> mods)
    {
        return GetStatBonus(mods, "armor");
    }

    public static float GetMoveSpeedBonus(List<ModData> mods)
    {
        return GetStatBonus(mods, "move_speed");
    }

    public static float GetCooldownReduction(List<ModData> mods)
    {
        return GetStatBonus(mods, "cooldown_reduction");
    }

    public static float GetSuperChargeBonus(List<ModData> mods)
    {
        return GetStatBonus(mods, "super_charge");
    }

    public static float GetGlimmerBonus(List<ModData> mods)
    {
        return GetStatBonus(mods, "glimmer_bonus");
    }

    public static float GetDropRateBonus(List<ModData> mods)
    {
        return GetStatBonus(mods, "drop_rate");
    }

    public static float GetElementDamageBonus(List<ModData> mods, string element)
    {
        return GetStatBonus(mods, $"{element}_damage");
    }
}
