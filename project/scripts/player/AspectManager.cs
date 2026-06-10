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

    /// <summary>
    /// 切换子职业时清空已装备星相
    /// </summary>
    public void ClearEquipped()
    {
        EquippedAspect1 = null;
        EquippedAspect2 = null;
    }
}
