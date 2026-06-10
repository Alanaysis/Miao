using Godot;
using System.Collections.Generic;
using Miao.Player;

namespace Miao.UI;

public partial class LevelUpUI : CanvasLayer
{
    [Signal]
    public delegate void FragmentSelectedEventHandler(int index);

    private List<FragmentConfig> _fragments = new();

    public void SetFragments(List<FragmentConfig> fragments)
    {
        _fragments = fragments;
    }

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;

        // 背景
        AddChild(UIStyle.Overlay(0.8f));

        // 标题
        var title = UIStyle.MakeLabel("选择碎片", 30, UIStyle.AccentGold, true);
        title.Position = new Vector2(0, 180);
        title.Size = new Vector2(1280, 40);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        AddChild(title);

        // 副标题
        var sub = UIStyle.MakeLabel("FRAGMENT UPGRADE", 14, UIStyle.TextMuted);
        sub.Position = new Vector2(0, 216);
        sub.Size = new Vector2(1280, 20);
        sub.HorizontalAlignment = HorizontalAlignment.Center;
        AddChild(sub);

        // 3 个碎片卡片
        float cardW = 260, cardH = 260, gap = 30;
        float totalW = cardW * _fragments.Count + gap * (_fragments.Count - 1);
        float startX = (1280 - totalW) / 2;

        for (int i = 0; i < _fragments.Count; i++)
        {
            CreateFragmentCard(_fragments[i], i, startX + i * (cardW + gap), 280, cardW, cardH);
        }
    }

    private void CreateFragmentCard(FragmentConfig fragment, int index, float x, float y, float w, float h)
    {
        // 解析稀有度颜色
        var rarityColor = new Color(GetRarityColorHex(fragment.Rarity));

        // 卡片面板
        var panel = UIStyle.Panel(new Vector2(w, h), rarityColor);
        panel.Position = new Vector2(x, y);
        AddChild(panel);

        // 稀有度条
        var rarityBar = new ColorRect();
        rarityBar.Size = new Vector2(w - 24, 3);
        rarityBar.Color = new Color(rarityColor.R, rarityColor.G, rarityColor.B, 0.8f);
        panel.AddChild(rarityBar);

        // 稀有度标签
        var rarityLabel = UIStyle.MakeLabel(GetRarityName(fragment.Rarity), 11, rarityColor, true);
        rarityLabel.Position = new Vector2(4, 10);
        rarityLabel.Size = new Vector2(w - 32, 16);
        rarityLabel.HorizontalAlignment = HorizontalAlignment.Right;
        panel.AddChild(rarityLabel);

        // 碎片名称
        var nameLabel = UIStyle.MakeLabel(fragment.Name, 20, rarityColor, true);
        nameLabel.Position = new Vector2(4, 28);
        nameLabel.Size = new Vector2(w - 32, 28);
        panel.AddChild(nameLabel);

        // 分隔线
        var sep = UIStyle.Separator(new Vector2(4, 58), w - 32, rarityColor);
        panel.AddChild(sep);

        // 描述
        var descLabel = UIStyle.MakeLabel(fragment.Description ?? "", 13, UIStyle.TextSecondary);
        descLabel.Position = new Vector2(4, 66);
        descLabel.Size = new Vector2(w - 32, 60);
        descLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        panel.AddChild(descLabel);

        // 效果列表
        float effectY = 130;
        if (fragment.Effects != null)
        {
            foreach (var effect in fragment.Effects)
            {
                var effectText = FormatEffect(effect);
                if (!string.IsNullOrEmpty(effectText))
                {
                    var effectLabel = UIStyle.MakeLabel(effectText, 12, UIStyle.TextPrimary);
                    effectLabel.Position = new Vector2(8, effectY);
                    effectLabel.Size = new Vector2(w - 40, 18);
                    panel.AddChild(effectLabel);
                    effectY += 20;
                }
            }
        }

        // 选择按钮
        var btn = UIStyle.MakeButton("选择", new Vector2(w - 32, 40));
        btn.Position = new Vector2(4, h - 56);
        btn.Size = new Vector2(w - 32, 40);
        btn.Pressed += () => EmitSignal(SignalName.FragmentSelected, index);
        panel.AddChild(btn);
    }

    private string FormatEffect(FragmentEffect effect)
    {
        return effect.Type switch
        {
            "stat" => FormatStatEffect(effect),
            "trigger" => FormatTriggerEffect(effect),
            "skill_duration" => $"技能持续时间 +{effect.Value}s",
            _ => ""
        };
    }

    private string FormatStatEffect(FragmentEffect effect)
    {
        string statName = effect.Stat switch
        {
            "move_speed" => "移动速度",
            "max_health" => "最大生命",
            "crit_chance" => "暴击率",
            "attack_speed" => "攻击速度",
            "damage" => "伤害",
            _ => effect.Stat
        };

        if (effect.Value > 0 && effect.Value < 1)
            return $"{statName} +{effect.Value * 100:F0}%";
        else if (effect.Value > 0)
            return $"{statName} +{effect.Value:F0}";
        else
            return $"{statName} {effect.Value:F0}";
    }

    private string FormatTriggerEffect(FragmentEffect effect)
    {
        string triggerName = effect.Trigger switch
        {
            "on_kill" => "击杀时",
            "on_crit" => "暴击时",
            "on_hit" => "命中时",
            "on_reload" => "换弹时",
            _ => effect.Trigger
        };

        string effectName = effect.Effect switch
        {
            "heal" => "恢复生命",
            "damage_boost" => "伤害提升",
            "speed_boost" => "移速提升",
            _ => effect.Effect
        };

        return $"{triggerName}: {effectName} +{effect.Value * 100:F0}%";
    }

    private string GetRarityColorHex(string rarity)
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

    private string GetRarityName(string rarity)
    {
        return rarity switch
        {
            "common" => "普通",
            "uncommon" => "优秀",
            "rare" => "稀有",
            "epic" => "史诗",
            "legendary" => "传说",
            _ => ""
        };
    }
}
