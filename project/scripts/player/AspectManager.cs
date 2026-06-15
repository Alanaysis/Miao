using Godot;
using System.Collections.Generic;
using System.Linq;
using Miao.Data;

namespace Miao.Player;

/// <summary>
/// 星相管理器 — 管理已装备的星相
/// 每个子职业最多装备 2 个星相，每个星相提供碎片槽位
/// </summary>
public partial class AspectManager : Node
{
    /// <summary>已装备的星相槽位 1</summary>
    public AspectConfig EquippedAspect1 { get; private set; }

    /// <summary>已装备的星相槽位 2</summary>
    public AspectConfig EquippedAspect2 { get; private set; }

    /// <summary>两个星相提供的碎片槽位总数</summary>
    public int TotalFragmentSlots =>
        (EquippedAspect1?.FragmentsSlots ?? 0) + (EquippedAspect2?.FragmentsSlots ?? 0);

    private Dictionary<string, Dictionary<string, List<AspectConfig>>> _allAspects;

    public override void _Ready()
    {
        LoadAspects();
    }

    private void LoadAspects()
    {
        _allAspects = DataLoader.Load<Dictionary<string, Dictionary<string, List<AspectConfig>>>>("aspects.json");
        if (_allAspects == null)
        {
            GD.PushWarning("AspectManager: aspects.json 加载失败或为空");
            _allAspects = new();
        }
    }

    /// <summary>
    /// 获取指定职业/子职业下所有可用的星相
    /// </summary>
    public List<AspectConfig> GetAvailableAspects(string className, string subclassKey)
    {
        if (_allAspects == null) return new List<AspectConfig>();
        if (_allAspects.ContainsKey(className) && _allAspects[className].ContainsKey(subclassKey))
        {
            return _allAspects[className][subclassKey];
        }
        return new List<AspectConfig>();
    }

    /// <summary>
    /// 装备星相到指定槽位（1 或 2）
    /// </summary>
    public bool EquipAspect(AspectConfig aspect, int slot)
    {
        if (aspect == null) return false;

        if (slot == 1)
        {
            EquippedAspect1 = aspect;
        }
        else if (slot == 2)
        {
            EquippedAspect2 = aspect;
        }
        else
        {
            return false;
        }

        GD.Print($"装备星相: {aspect.Name} (槽位 {slot})");
        return true;
    }

    /// <summary>
    /// 卸下指定槽位的星相
    /// </summary>
    public void UnequipAspect(int slot)
    {
        if (slot == 1) EquippedAspect1 = null;
        else if (slot == 2) EquippedAspect2 = null;
    }

    /// <summary>
    /// 获取所有已装备的星相列表
    /// </summary>
    public List<AspectConfig> GetEquippedAspects()
    {
        var list = new List<AspectConfig>();
        if (EquippedAspect1 != null) list.Add(EquippedAspect1);
        if (EquippedAspect2 != null) list.Add(EquippedAspect2);
        return list;
    }

    /// <summary>
    /// 检查是否装备了指定 ID 的星相
    /// </summary>
    public bool HasAspect(string aspectId)
    {
        return (EquippedAspect1?.Id == aspectId) || (EquippedAspect2?.Id == aspectId);
    }

    // ==================== 星相效果查询 ====================

    /// <summary>
    /// 闪现后是否获得隐身（消隐步）
    /// </summary>
    public bool HasBlinkInvis() => HasAspect("aspect_vanishing_step");

    /// <summary>
    /// 翻滚后是否获得增幅（流动状态）
    /// </summary>
    public bool HasRollEmpower() => HasAspect("aspect_flow_state");

    /// <summary>
    /// 标记技能是否致盲敌人（陷阱伏击）
    /// </summary>
    public bool HasMarkBlind() => HasAspect("aspect_trappers_ambush");

    /// <summary>
    /// 近战击杀是否延长雷击范围（组合打击）
    /// </summary>
    public bool HasMeleeExtendLightning() => HasAspect("aspect_combination_blow");

    /// <summary>
    /// 滑铲近战是否释放电弧波（暴风打击）
    /// </summary>
    public bool HasSlideLightning() => HasAspect("aspect_tempest_strike");

    /// <summary>
    /// 精准击杀是否提升射速（瞄准就绪）
    /// </summary>
    public bool HasPrecisionFireRate() => HasAspect("aspect_on_your_mark");

    /// <summary>
    /// 燃烧弹是否投掷3枚（飞刀戏法）
    /// </summary>
    public bool HasTripleGrenade() => HasAspect("aspect_knives_trick");

    /// <summary>
    /// 技能击杀是否生成可投掷炸弹（火药赌注）
    /// </summary>
    public bool HasGambleBomb() => HasAspect("aspect_gunpowder_gamble");

    /// <summary>
    /// 技能击杀是否生成烈日区域（无敌烈日 - 泰坦）
    /// </summary>
    public bool HasSolarArea() => HasAspect("aspect_sol_invictus");

    /// <summary>
    /// 技能伤害是否可叠加（咆哮烈焰 - 泰坦）
    /// </summary>
    public bool HasStackingDamage() => HasAspect("aspect_roaring_flames");

    /// <summary>
    /// 滑铲近战是否释放火焰波（献祭 - 泰坦）
    /// </summary>
    public bool HasSlideFire() => HasAspect("aspect_consecration");

    /// <summary>
    /// 破盾后近战是否增强（击倒 - 泰坦）
    /// </summary>
    public bool HasShieldBreakMelee() => HasAspect("aspect_knockout");

    /// <summary>
    /// 冲刺时是否获得护盾（主宰 - 泰坦）
    /// </summary>
    public bool HasSprintShield() => HasAspect("aspect_juggernaut");

    /// <summary>
    /// 手雷伤害是否增强（雷霆之触 - 泰坦）
    /// </summary>
    public bool HasEnhancedGrenade() => HasAspect("aspect_touch_of_thunder");

    /// <summary>
    /// 切换子职业时清空已装备星相
    /// </summary>
    public void ClearEquipped()
    {
        EquippedAspect1 = null;
        EquippedAspect2 = null;
    }
}
