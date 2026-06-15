using Godot;
using System.Collections.Generic;
using System.Linq;
using Miao.Data;

namespace Miao.Player;

public partial class FragmentManager : Node
{
    public List<FragmentConfig> CollectedFragments { get; private set; } = new();
    public int FragmentCount => CollectedFragments.Count;

    // All fragments for the current subclass
    private List<FragmentConfig> _availableFragments = new();
    private string _currentClass = "hunter";
    private string _currentSubclass = "void";

    public override void _Ready()
    {
    }

    public void LoadFragmentsForSubclass(string className, string subclassKey)
    {
        _currentClass = className;
        _currentSubclass = subclassKey;

        var allFragments = DataLoader.Load<Dictionary<string, Dictionary<string, List<FragmentConfig>>>>("fragments.json");
        if (allFragments != null && allFragments.ContainsKey(className) && allFragments[className].ContainsKey(subclassKey))
        {
            _availableFragments = allFragments[className][subclassKey];
        }
    }

    public List<FragmentConfig> RollRandomFragments(int count)
    {
        var available = _availableFragments
            .Where(f => !CollectedFragments.Any(c => c.Id == f.Id))
            .ToList();

        if (available.Count == 0) return new List<FragmentConfig>();

        var result = new List<FragmentConfig>();
        var rng = new global::System.Random();

        for (int i = 0; i < count && available.Count > 0; i++)
        {
            int index = rng.Next(available.Count);
            result.Add(available[index]);
            available.RemoveAt(index);
        }

        return result;
    }

    public void AddFragment(FragmentConfig fragment)
    {
        CollectedFragments.Add(fragment);
        ApplyFragment(fragment);
    }

    private void ApplyFragment(FragmentConfig fragment)
    {
        var player = GetParent<Player>();
        if (player == null) return;

        foreach (var effect in fragment.Effects)
        {
            switch (effect.Type)
            {
                case "stat":
                    ApplyStatEffect(player, effect);
                    break;
                case "trigger":
                    // Triggers are handled in combat/kill callbacks
                    break;
                case "skill_duration":
                    // Applied when calculating skill durations
                    break;
            }
        }
    }

    private void ApplyStatEffect(Player player, FragmentEffect effect)
    {
        switch (effect.Stat)
        {
            case "move_speed":
                player.MoveSpeed *= (1 + effect.Value);
                break;
            case "max_health":
                player.MaxHealth += Mathf.RoundToInt(effect.Value);
                player.Heal(Mathf.RoundToInt(effect.Value));
                break;
            case "crit_chance":
                // Handled in PerkSystem
                break;
        }
    }

    public float GetStatBonus(string stat)
    {
        float bonus = 0;
        foreach (var fragment in CollectedFragments)
        {
            foreach (var effect in fragment.Effects)
            {
                if (effect.Type == "stat" && effect.Stat == stat)
                {
                    bonus += effect.Value;
                }
            }
        }
        return bonus;
    }

    public float GetTriggerMultiplier(string trigger, string effect)
    {
        float mult = 1.0f;
        foreach (var fragment in CollectedFragments)
        {
            foreach (var fragEffect in fragment.Effects)
            {
                if (fragEffect.Type == "trigger" && fragEffect.Trigger == trigger && fragEffect.Effect == effect)
                {
                    mult += fragEffect.Value;
                }
            }
        }
        return mult;
    }

    /// <summary>
    /// 处理触发器效果 — 在击杀、命中等事件时调用
    /// </summary>
    public void ProcessTrigger(string trigger, object context = null)
    {
        var player = GetParent<Player>();
        if (player == null) return;

        foreach (var fragment in CollectedFragments)
        {
            foreach (var effect in fragment.Effects)
            {
                if (effect.Type == "trigger" && effect.Trigger == trigger)
                {
                    ApplyTriggerEffect(player, effect, context);
                }
            }
        }
    }

    private void ApplyTriggerEffect(Player player, FragmentEffect effect, object context)
    {
        switch (effect.Effect)
        {
            case "heal":
                player.Heal(Mathf.RoundToInt(effect.Value));
                break;

            case "damage_boost":
                player.ApplyEmpower(effect.Value, effect.Duration);
                break;

            case "invisible":
                player.ApplyInvisibility(effect.Duration);
                break;

            case "reload_speed":
                // 装填速度加成（简化：直接应用到下次射击的射速）
                // TODO: 接入实际装填速度系统
                break;

            case "grenade_energy":
                // 回复技能2能量（简化）
                player.ReduceSkill2Cooldown(effect.Value);
                break;

            case "refund_super":
                player.AddSuperCharge(effect.Value * player.SuperMaxCharge);
                break;

            case "ignite":
                // 点燃周围敌人
                if (context is Godot.Vector2 pos)
                {
                    float radius = effect.Value; // 用 value 作为半径
                    foreach (var node in player.GetTree().GetNodesInGroup("enemy"))
                    {
                        if (node is Enemy.Enemy enemy)
                        {
                            float dist = pos.DistanceTo(enemy.GlobalPosition);
                            if (dist < radius)
                            {
                                enemy.TakeDamage(10); // 简化：固定伤害
                            }
                        }
                    }
                }
                break;

            case "cure":
                player.Heal(Mathf.RoundToInt(effect.Value));
                break;

            case "radiant":
                player.ApplyEmpower(0.15f, effect.Duration);
                break;
        }
    }

    public void Clear()
    {
        CollectedFragments.Clear();
    }

    public string GetRarityColor(string rarity)
    {
        return rarity switch
        {
            "common" => "#9ca3af",
            "uncommon" => "#22c55e",
            "rare" => "#3b82f6",
            "epic" => "#a855f7",
            "legendary" => "#eab308",
            _ => "#ffffff"
        };
    }
}
