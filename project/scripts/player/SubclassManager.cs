using Godot;
using System.Collections.Generic;
using Miao.Data;

namespace Miao.Player;

public partial class SubclassManager : Node
{
    public SubclassType ActiveSubclass { get; private set; } = SubclassType.Void;
    public SubclassConfig ActiveConfig { get; private set; }
    public string ClassName { get; private set; } = "hunter";

    private Dictionary<string, Dictionary<string, SubclassConfig>> _allSubclasses;

    public override void _Ready()
    {
        LoadSubclasses();
    }

    private void LoadSubclasses()
    {
        // Load from JSON
        var json = DataLoader.Load<Dictionary<string, Dictionary<string, SubclassConfig>>>("subclasses.json");
        if (json != null)
        {
            _allSubclasses = json;
            // 不在此处调用 SetSubclass —— 等 SetClassName 被 Player._Ready 调用后再加载
        }
    }

    /// <summary>
    /// 设置当前职业名称（由 Player 子类在 _Ready 中调用）
    /// </summary>
    public void SetClassName(string className)
    {
        ClassName = className;
        SetSubclass(ActiveSubclass);
    }

    public void SetSubclass(SubclassType type)
    {
        ActiveSubclass = type;
        string key = type.ToString().ToLower();

        if (_allSubclasses != null && _allSubclasses.ContainsKey(ClassName) && _allSubclasses[ClassName].ContainsKey(key))
        {
            ActiveConfig = _allSubclasses[ClassName][key];
        }
    }

    public void SetSubclassForClass(string className, SubclassType type)
    {
        ActiveSubclass = type;
        string key = type.ToString().ToLower();

        if (_allSubclasses != null && _allSubclasses.ContainsKey(className) && _allSubclasses[className].ContainsKey(key))
        {
            ActiveConfig = _allSubclasses[className][key];
        }
    }

    public SkillConfig GetSkill1Config()
    {
        return ActiveConfig?.Skills?.GetValueOrDefault("skill1");
    }

    public SkillConfig GetSkill2Config()
    {
        return ActiveConfig?.Skills?.GetValueOrDefault("skill2");
    }

    public SuperConfig GetSuperConfig()
    {
        return ActiveConfig?.Super;
    }

    public Color GetElementColor()
    {
        return ActiveSubclass switch
        {
            SubclassType.Void => new Color("#8b5cf6"),
            SubclassType.Arc => new Color("#3b82f6"),
            SubclassType.Solar => new Color("#f97316"),
            SubclassType.Stasis => new Color("#e2e8f0"),
            _ => Colors.White
        };
    }

    public List<SubclassType> GetAvailableSubclasses()
    {
        return new List<SubclassType> { SubclassType.Void, SubclassType.Arc, SubclassType.Solar };
    }

    /// <summary>
    /// 获取当前职业名称（用于星相/碎片查询）
    /// </summary>
    public string GetClassName() => ClassName;

    /// <summary>
    /// 获取当前子职业键名（用于星相/碎片查询）
    /// </summary>
    public string GetSubclassKey() => ActiveSubclass.ToString().ToLower();
}
