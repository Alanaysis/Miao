using Godot;

namespace Miao.System;

/// <summary>
/// 地图背景
/// 绘制网格线、装饰物和房间主题色
/// 支持基于地图主题的视觉风格
/// </summary>
public partial class MapBackground : Node2D
{
    [Export] public int GridSize = 100;
    [Export] public float MapWidth = 2000f;
    [Export] public float MapHeight = 2000f;
    [Export] public int DecorationCount = 60;

    private Vector2[] _decorations;
    private Color[] _decorationColors;
    private int _currentTheme = 0;
    private string _currentMapTheme = "hive";

    // 房间主题：背景色、网格色、装饰色、边框色
    private static readonly Color[][] RoomThemes = new Color[][]
    {
        // 房间1：草地
        new[] { new Color(0.08f, 0.12f, 0.06f), new Color(0.15f, 0.22f, 0.12f, 0.4f), new Color(0.12f, 0.25f, 0.1f, 0.5f), new Color(0.2f, 0.35f, 0.15f) },
        // 房间2：荒地
        new[] { new Color(0.14f, 0.11f, 0.07f), new Color(0.25f, 0.2f, 0.12f, 0.4f), new Color(0.22f, 0.18f, 0.1f, 0.5f), new Color(0.35f, 0.28f, 0.15f) },
        // 房间3：洞穴
        new[] { new Color(0.06f, 0.06f, 0.1f), new Color(0.12f, 0.12f, 0.2f, 0.4f), new Color(0.1f, 0.1f, 0.18f, 0.5f), new Color(0.2f, 0.2f, 0.35f) },
        // 房间4：虫巢
        new[] { new Color(0.12f, 0.06f, 0.08f), new Color(0.22f, 0.1f, 0.15f, 0.4f), new Color(0.2f, 0.08f, 0.12f, 0.5f), new Color(0.35f, 0.15f, 0.2f) },
        // Boss房：深渊
        new[] { new Color(0.04f, 0.04f, 0.08f), new Color(0.1f, 0.08f, 0.18f, 0.4f), new Color(0.08f, 0.06f, 0.15f, 0.5f), new Color(0.2f, 0.12f, 0.35f) },
    };

    // 地图主题色板：背景色、网格色、装饰色、边框色
    // 绿/棕有机 (hive), 灰/棕废墟 (wasteland), 深灰洞穴 (cave), 紫/暗蓝深渊 (void), 红/橙地狱 (solar)
    private static readonly Color[][] MapThemeColors = new Color[][]
    {
        // hive (虫巢): Green/brown, organic shapes
        new[] { new Color(0.06f, 0.1f, 0.04f), new Color(0.1f, 0.2f, 0.08f, 0.4f), new Color(0.15f, 0.3f, 0.08f, 0.5f), new Color(0.2f, 0.35f, 0.1f) },
        // wasteland (废墟): Grey/brown, geometric debris
        new[] { new Color(0.1f, 0.09f, 0.07f), new Color(0.2f, 0.18f, 0.14f, 0.4f), new Color(0.25f, 0.2f, 0.14f, 0.5f), new Color(0.35f, 0.3f, 0.2f) },
        // cave (洞穴): Dark grey, stalactites
        new[] { new Color(0.05f, 0.05f, 0.08f), new Color(0.1f, 0.1f, 0.16f, 0.4f), new Color(0.12f, 0.12f, 0.2f, 0.5f), new Color(0.2f, 0.2f, 0.3f) },
        // void (深渊): Purple/dark blue, void particles
        new[] { new Color(0.04f, 0.03f, 0.08f), new Color(0.1f, 0.06f, 0.2f, 0.4f), new Color(0.15f, 0.08f, 0.28f, 0.5f), new Color(0.25f, 0.12f, 0.4f) },
        // solar (地狱): Red/orange, lava effects
        new[] { new Color(0.12f, 0.04f, 0.02f), new Color(0.25f, 0.1f, 0.05f, 0.4f), new Color(0.35f, 0.12f, 0.05f, 0.5f), new Color(0.5f, 0.2f, 0.08f) },
    };

    private static readonly string[] MapThemeIds = { "hive", "wasteland", "cave", "void", "solar" };

    public override void _Ready()
    {
        ZIndex = -10;
        GenerateDecorations();
    }

    public void SetRoomTheme(int roomIndex)
    {
        _currentTheme = Mathf.Clamp(roomIndex, 0, RoomThemes.Length - 1);
        GenerateDecorations();
        QueueRedraw();
    }

    /// <summary>
    /// 设置地图主题（根据地图 theme 字符串）
    /// </summary>
    public void SetMapTheme(string themeId)
    {
        _currentMapTheme = themeId ?? "hive";
        GenerateDecorations();
        QueueRedraw();
    }

    private int GetMapThemeIndex()
    {
        for (int i = 0; i < MapThemeIds.Length; i++)
        {
            if (MapThemeIds[i] == _currentMapTheme) return i;
        }
        return 0; // Default to hive
    }

    private Color[] GetCurrentColors()
    {
        // Use map theme colors as base, blend with room theme for variation
        int mapIdx = GetMapThemeIndex();
        var mapColors = MapThemeColors[mapIdx];
        var roomColors = RoomThemes[_currentTheme];

        // Blend: 70% map theme + 30% room theme for subtle per-room variation
        return new Color[]
        {
            BlendColor(mapColors[0], roomColors[0], 0.7f),
            BlendColor(mapColors[1], roomColors[1], 0.7f),
            BlendColor(mapColors[2], roomColors[2], 0.7f),
            BlendColor(mapColors[3], roomColors[3], 0.7f),
        };
    }

    private static Color BlendColor(Color a, Color b, float aWeight)
    {
        float bWeight = 1f - aWeight;
        return new Color(
            a.R * aWeight + b.R * bWeight,
            a.G * aWeight + b.G * bWeight,
            a.B * aWeight + b.B * bWeight,
            a.A * aWeight + b.A * bWeight
        );
    }

    private void GenerateDecorations()
    {
        _decorations = new Vector2[DecorationCount];
        _decorationColors = new Color[DecorationCount];

        var rng = new RandomNumberGenerator();
        rng.Randomize();

        var colors = GetCurrentColors();
        var baseColor = colors[2];

        for (int i = 0; i < DecorationCount; i++)
        {
            _decorations[i] = new Vector2(
                rng.RandfRange(50, MapWidth - 50),
                rng.RandfRange(50, MapHeight - 50)
            );

            float variation = rng.RandfRange(0.7f, 1.3f);
            _decorationColors[i] = new Color(
                baseColor.R * variation,
                baseColor.G * variation,
                baseColor.B * variation,
                baseColor.A
            );
        }
    }

    public override void _Draw()
    {
        var colors = GetCurrentColors();
        var bgColor = colors[0];
        var gridColor = colors[1];
        var borderColor = colors[3];

        // 背景色
        DrawRect(new Rect2(0, 0, MapWidth, MapHeight), bgColor);

        // 网格线
        for (float x = 0; x <= MapWidth; x += GridSize)
            DrawLine(new Vector2(x, 0), new Vector2(x, MapHeight), gridColor, 1f);
        for (float y = 0; y <= MapHeight; y += GridSize)
            DrawLine(new Vector2(0, y), new Vector2(MapWidth, y), gridColor, 1f);

        // 装饰物
        if (_decorations != null)
        {
            for (int i = 0; i < _decorations.Length; i++)
            {
                float radius = 6f + (i % 7) * 3f;
                DrawMapDecoration(i, radius);
            }
        }

        // 边界
        DrawRect(new Rect2(0, 0, MapWidth, MapHeight), borderColor, false, 4f);

        // 内部装饰边框
        float inset = 30;
        var innerColor = new Color(borderColor.R, borderColor.G, borderColor.B, 0.3f);
        DrawRect(new Rect2(inset, inset, MapWidth - inset * 2, MapHeight - inset * 2), innerColor, false, 1f);
    }

    /// <summary>
    /// 根据地图主题绘制不同形状的装饰物
    /// </summary>
    private void DrawMapDecoration(int index, float radius)
    {
        var pos = _decorations[index];
        var color = _decorationColors[index];

        switch (_currentMapTheme)
        {
            case "hive":
                // 虫巢：有机形状（椭圆/虫卵）
                DrawCircle(pos, radius * 1.3f, color);
                // 小虫卵装饰
                if (index % 5 == 0)
                    DrawCircle(pos + new Vector2(radius * 0.8f, 0), radius * 0.5f,
                        new Color(color.R * 0.8f, color.G * 1.2f, color.B * 0.8f, color.A));
                break;

            case "wasteland":
                // 废墟：几何碎片（矩形/菱形）
                if (index % 3 == 0)
                {
                    // 菱形
                    var points = new Vector2[]
                    {
                        pos + new Vector2(0, -radius),
                        pos + new Vector2(radius, 0),
                        pos + new Vector2(0, radius),
                        pos + new Vector2(-radius, 0),
                    };
                    DrawColoredPolygon(points, color);
                }
                else
                {
                    DrawRect(new Rect2(pos - new Vector2(radius, radius * 0.6f),
                        new Vector2(radius * 2, radius * 1.2f)), color);
                }
                break;

            case "cave":
                // 洞穴：石笋/钟乳石（三角形和矩形）
                if (index % 2 == 0)
                {
                    // 石笋（向上三角）
                    var points = new Vector2[]
                    {
                        pos + new Vector2(-radius * 0.6f, radius),
                        pos + new Vector2(0, -radius * 1.5f),
                        pos + new Vector2(radius * 0.6f, radius),
                    };
                    DrawColoredPolygon(points, color);
                }
                else
                {
                    // 石块
                    DrawRect(new Rect2(pos - new Vector2(radius, radius),
                        new Vector2(radius * 2, radius * 2)), color);
                }
                break;

            case "void":
                // 深渊：虚空粒子（小圆点 + 十字）
                DrawCircle(pos, radius * 0.6f, color);
                // 十字光芒
                var crossColor = new Color(color.R * 1.5f, color.G * 0.8f, color.B * 1.5f, color.A * 0.6f);
                DrawLine(pos + new Vector2(-radius, 0), pos + new Vector2(radius, 0), crossColor, 1f);
                DrawLine(pos + new Vector2(0, -radius), pos + new Vector2(0, radius), crossColor, 1f);
                break;

            case "solar":
                // 地狱：熔岩效果（红橙色圆 + 光晕）
                DrawCircle(pos, radius, color);
                // 光晕
                var glowColor = new Color(1f, 0.5f, 0.1f, 0.2f);
                DrawCircle(pos, radius * 1.8f, glowColor);
                break;

            default:
                DrawCircle(pos, radius, color);
                break;
        }
    }
}
