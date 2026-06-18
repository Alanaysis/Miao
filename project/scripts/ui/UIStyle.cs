using Godot;
using Miao.Weapon;

namespace Miao.UI;

/// <summary>
/// UI 统一样式工具类 — 暗色科幻风格
/// 所有 UI 共用，保证视觉一致
/// </summary>
public static class UIStyle
{
    // ==================== 配色 ====================
    public static readonly Color BgDark = new("#0a0e14");
    public static readonly Color BgPanel = new("#111827");
    public static readonly Color BgPanelLight = new("#1e293b");
    public static readonly Color BorderSubtle = new("#1e293b");
    public static readonly Color BorderGlow = new("#3b82f6");
    public static readonly Color TextPrimary = new("#e2e8f0");
    public static readonly Color TextSecondary = new("#94a3b8");
    public static readonly Color TextMuted = new("#64748b");
    public static readonly Color AccentBlue = new("#3b82f6");
    public static readonly Color AccentCyan = new("#06b6d4");
    public static readonly Color AccentGold = new("#f59e0b");
    public static readonly Color AccentGreen = new("#22c55e");
    public static readonly Color AccentRed = new("#ef4444");
    public static readonly Color AccentPurple = new("#a855f7");
    public static readonly Color HealthGreen = new("#22c55e");
    public static readonly Color HealthYellow = new("#eab308");
    public static readonly Color HealthRed = new("#ef4444");

    // ==================== 字体 ====================
    private static Font _fontBold;
    private static Font _fontRegular;

    public static Font FontBold
    {
        get
        {
            if (_fontBold == null)
            {
                _fontBold = GD.Load<Font>("res://assets/fonts/Rajdhani-Bold.ttf");
                if (_fontBold == null) _fontBold = ThemeDB.FallbackFont;
            }
            return _fontBold;
        }
    }

    public static Font FontRegular
    {
        get
        {
            if (_fontRegular == null)
            {
                _fontRegular = GD.Load<Font>("res://assets/fonts/Rajdhani-Regular.ttf");
                if (_fontRegular == null) _fontRegular = ThemeDB.FallbackFont;
            }
            return _fontRegular;
        }
    }

    // ==================== 稀有度颜色 ====================
    public static Color RarityColor(Rarity rarity) => rarity switch
    {
        Rarity.Common => new Color("#9ca3af"),
        Rarity.Uncommon => new Color("#22c55e"),
        Rarity.Rare => new Color("#3b82f6"),
        Rarity.Epic => new Color("#a855f7"),
        Rarity.Legendary => new Color("#f59e0b"),
        Rarity.Exotic => new Color("#ef4444"),
        _ => TextPrimary
    };

    public static string RarityName(Rarity rarity) => rarity switch
    {
        Rarity.Common => "普通",
        Rarity.Uncommon => "优秀",
        Rarity.Rare => "稀有",
        Rarity.Epic => "史诗",
        Rarity.Legendary => "传说",
        Rarity.Exotic => "异域",
        _ => ""
    };

    // ==================== 组件工厂 ====================

    /// <summary>创建带发光边框的暗色面板</summary>
    public static PanelContainer Panel(Vector2 size, Color? borderColor = null, Color? bgColor = null)
    {
        var border = borderColor ?? BorderGlow;
        var bg = bgColor ?? BgPanel;

        var panel = new PanelContainer();
        panel.CustomMinimumSize = size;

        var style = new StyleBoxFlat();
        style.BgColor = new Color(bg.R, bg.G, bg.B, 0.92f);
        style.BorderColor = new Color(border.R, border.G, border.B, 0.6f);
        style.BorderWidthLeft = 1; style.BorderWidthRight = 1;
        style.BorderWidthTop = 1; style.BorderWidthBottom = 1;
        style.CornerRadiusTopLeft = 6; style.CornerRadiusTopRight = 6;
        style.CornerRadiusBottomLeft = 6; style.CornerRadiusBottomRight = 6;
        style.ContentMarginLeft = 12; style.ContentMarginRight = 12;
        style.ContentMarginTop = 8; style.ContentMarginBottom = 8;
        panel.AddThemeStyleboxOverride("panel", style);

        return panel;
    }

    /// <summary>创建渐变进度条</summary>
    public static ProgressBar Bar(Vector2 pos, Vector2 size, Color fillColor, Color? bgColor = null)
    {
        var bar = new ProgressBar();
        bar.Position = pos;
        bar.Size = size;
        bar.MaxValue = 100;
        bar.Value = 100;
        bar.ShowPercentage = false;

        // 背景
        var bgStyle = new StyleBoxFlat();
        bgStyle.BgColor = bgColor ?? new Color("#0f172a");
        bgStyle.CornerRadiusTopLeft = 3; bgStyle.CornerRadiusTopRight = 3;
        bgStyle.CornerRadiusBottomLeft = 3; bgStyle.CornerRadiusBottomRight = 3;
        bar.AddThemeStyleboxOverride("background", bgStyle);

        // 填充
        var fillStyle = new StyleBoxFlat();
        fillStyle.BgColor = fillColor;
        fillStyle.CornerRadiusTopLeft = 3; fillStyle.CornerRadiusTopRight = 3;
        fillStyle.CornerRadiusBottomLeft = 3; fillStyle.CornerRadiusBottomRight = 3;
        bar.AddThemeStyleboxOverride("fill", fillStyle);

        return bar;
    }

    /// <summary>更新进度条填充色</summary>
    public static void UpdateBarColor(ProgressBar bar, Color color)
    {
        var style = new StyleBoxFlat();
        style.BgColor = color;
        style.CornerRadiusTopLeft = 3; style.CornerRadiusTopRight = 3;
        style.CornerRadiusBottomLeft = 3; style.CornerRadiusBottomRight = 3;
        bar.AddThemeStyleboxOverride("fill", style);
    }

    /// <summary>创建标签</summary>
    public static Label MakeLabel(string text, int fontSize = 14, Color? color = null, bool bold = false)
    {
        var label = new Label();
        label.Text = text;
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", color ?? TextPrimary);
        label.AddThemeFontOverride("font", bold ? FontBold : FontRegular);
        return label;
    }

    /// <summary>创建带位置的标签</summary>
    public static Label LabelAt(Vector2 pos, string text, int fontSize = 14, Color? color = null, bool bold = false)
    {
        var label = MakeLabel(text, fontSize, color, bold);
        label.Position = pos;
        return label;
    }

    /// <summary>创建科技风按钮</summary>
    public static Button MakeButton(string text, Vector2 size)
    {
        var btn = new Button();
        btn.Text = text;
        btn.CustomMinimumSize = size;

        var normal = new StyleBoxFlat();
        normal.BgColor = new Color("#1e293b");
        normal.BorderColor = new Color("#3b82f6", 0.5f);
        normal.BorderWidthLeft = 1; normal.BorderWidthRight = 1;
        normal.BorderWidthTop = 1; normal.BorderWidthBottom = 1;
        normal.CornerRadiusTopLeft = 4; normal.CornerRadiusTopRight = 4;
        normal.CornerRadiusBottomLeft = 4; normal.CornerRadiusBottomRight = 4;
        normal.ContentMarginLeft = 8; normal.ContentMarginRight = 8;
        normal.ContentMarginTop = 4; normal.ContentMarginBottom = 4;
        btn.AddThemeStyleboxOverride("normal", normal);

        var hover = new StyleBoxFlat();
        hover.BgColor = new Color("#1e3a5f");
        hover.BorderColor = new Color("#3b82f6");
        hover.BorderWidthLeft = 1; hover.BorderWidthRight = 1;
        hover.BorderWidthTop = 1; hover.BorderWidthBottom = 1;
        hover.CornerRadiusTopLeft = 4; hover.CornerRadiusTopRight = 4;
        hover.CornerRadiusBottomLeft = 4; hover.CornerRadiusBottomRight = 4;
        hover.ContentMarginLeft = 8; hover.ContentMarginRight = 8;
        hover.ContentMarginTop = 4; hover.ContentMarginBottom = 4;
        btn.AddThemeStyleboxOverride("hover", hover);

        var pressed = new StyleBoxFlat();
        pressed.BgColor = new Color("#0f2942");
        pressed.BorderColor = new Color("#60a5fa");
        pressed.BorderWidthLeft = 1; pressed.BorderWidthRight = 1;
        pressed.BorderWidthTop = 1; pressed.BorderWidthBottom = 1;
        pressed.CornerRadiusTopLeft = 4; pressed.CornerRadiusTopRight = 4;
        pressed.CornerRadiusBottomLeft = 4; pressed.CornerRadiusBottomRight = 4;
        pressed.ContentMarginLeft = 8; pressed.ContentMarginRight = 8;
        pressed.ContentMarginTop = 4; pressed.ContentMarginBottom = 4;
        btn.AddThemeStyleboxOverride("pressed", pressed);

        btn.AddThemeFontOverride("font", FontBold);
        btn.AddThemeFontSizeOverride("font_size", 15);
        btn.AddThemeColorOverride("font_color", TextPrimary);

        return btn;
    }

    /// <summary>创建分隔线</summary>
    public static ColorRect Separator(Vector2 pos, float width, Color? color = null)
    {
        var sep = new ColorRect();
        sep.Position = pos;
        sep.Size = new Vector2(width, 1);
        sep.Color = color ?? new Color("#334155");
        return sep;
    }

    /// <summary>创建技能方块（带冷却遮罩）</summary>
    public static Control SkillSlot(Vector2 pos, string key, Color accentColor)
    {
        var root = new Control();
        root.Position = pos;
        root.Size = new Vector2(48, 48);

        // 背景
        var bg = new ColorRect();
        bg.Size = new Vector2(48, 48);
        bg.Color = new Color("#111827");
        root.AddChild(bg);

        // 边框
        var border = new ColorRect();
        border.Size = new Vector2(48, 48);
        border.Color = new Color(accentColor.R, accentColor.G, accentColor.B, 0.4f);
        border.MouseFilter = Control.MouseFilterEnum.Ignore;
        root.AddChild(border);

        // 内部背景
        var inner = new ColorRect();
        inner.Position = new Vector2(1, 1);
        inner.Size = new Vector2(46, 46);
        inner.Color = new Color("#0f172a");
        root.AddChild(inner);

        // 按键文字
        var label = MakeLabel(key, 18, accentColor, true);
        label.Position = new Vector2(12, 4);
        label.Size = new Vector2(24, 24);
        label.HorizontalAlignment = HorizontalAlignment.Center;
        root.AddChild(label);

        // 冷却遮罩（从下往上填充）
        var mask = new ColorRect();
        mask.Position = new Vector2(1, 1);
        mask.Size = new Vector2(46, 0); // 高度动态设置
        mask.Color = new Color(0, 0, 0, 0.7f);
        mask.Name = "CooldownMask";
        root.AddChild(mask);

        // 状态文字
        var statusLabel = MakeLabel("就绪", 10, new Color("#22c55e"));
        statusLabel.Position = new Vector2(0, 30);
        statusLabel.Size = new Vector2(48, 14);
        statusLabel.HorizontalAlignment = HorizontalAlignment.Center;
        statusLabel.Name = "StatusLabel";
        root.AddChild(statusLabel);

        return root;
    }

    /// <summary>全屏半透明遮罩（自适应分辨率）</summary>
    public static ColorRect Overlay(float alpha = 0.85f)
    {
        var bg = new ColorRect();
        bg.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        bg.Color = new Color(0, 0, 0, alpha);
        bg.MouseFilter = Control.MouseFilterEnum.Ignore;
        return bg;
    }

    /// <summary>创建带 Tween 动画的进度条（支持颜色渐变）</summary>
    public static ProgressBar AnimatedBar(Vector2 pos, Vector2 size, Color fillColor, Color? bgColor = null)
    {
        var bar = Bar(pos, size, fillColor, bgColor);
        bar.Name = "AnimatedBar";
        return bar;
    }

    /// <summary>平滑更新进度条值（带 Tween 动画）</summary>
    public static void SmoothUpdateBar(ProgressBar bar, float targetValue, float duration = 0.3f)
    {
        if (bar == null) return;

        // 先杀掉旧动画
        if (bar.HasMeta("tween"))
        {
            var existingTween = (Tween)bar.GetMeta("tween");
            existingTween?.Kill();
        }

        var tween = bar.CreateTween();
        tween.TweenProperty(bar, "value", targetValue, duration);
        tween.SetTrans(Tween.TransitionType.Cubic);
        tween.SetEase(Tween.EaseType.Out);
        bar.SetMeta("tween", tween);
    }

    /// <summary>脉冲动画（用于超能条满时闪烁）</summary>
    public static Tween Pulse(Control target, Color pulseColor, float duration = 0.6f)
    {
        var tween = target.CreateTween().SetLoops();
        tween.TweenProperty(target, "modulate", pulseColor, duration / 2);
        tween.TweenProperty(target, "modulate", Colors.White, duration / 2);
        return tween;
    }

    /// <summary>全屏闪烁效果（受击反馈）</summary>
    public static void ScreenFlash(CanvasLayer parent, Color color, float duration = 0.15f)
    {
        var flash = new ColorRect();
        flash.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        flash.Color = new Color(color.R, color.G, color.B, 0.3f);
        flash.MouseFilter = Control.MouseFilterEnum.Ignore;
        flash.ZIndex = 99;
        parent.AddChild(flash);

        var tween = parent.CreateTween();
        tween.TweenProperty(flash, "color:a", 0.0f, duration);
        tween.SetTrans(Tween.TransitionType.Quad);
        tween.SetEase(Tween.EaseType.Out);
        tween.TweenCallback(Callable.From(() => flash.QueueFree()));
    }
}
