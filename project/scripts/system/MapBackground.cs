using Godot;

namespace Miao.System;

/// <summary>
/// 地图背景
/// 绘制网格线、装饰物和房间主题色
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

    private void GenerateDecorations()
    {
        _decorations = new Vector2[DecorationCount];
        _decorationColors = new Color[DecorationCount];

        var rng = new RandomNumberGenerator();
        rng.Randomize();

        var theme = RoomThemes[_currentTheme];
        var baseColor = theme[2];

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
        var theme = RoomThemes[_currentTheme];
        var bgColor = theme[0];
        var gridColor = theme[1];
        var borderColor = theme[3];

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
                // 不同房间不同形状
                if (_currentTheme == 2) // 洞穴：石头（方块）
                {
                    DrawRect(new Rect2(_decorations[i] - new Vector2(radius, radius), new Vector2(radius * 2, radius * 2)), _decorationColors[i]);
                }
                else if (_currentTheme == 3) // 虫巢：有机物（椭圆）
                {
                    DrawCircle(_decorations[i], radius * 1.3f, _decorationColors[i]);
                }
                else
                {
                    DrawCircle(_decorations[i], radius, _decorationColors[i]);
                }
            }
        }

        // 边界
        DrawRect(new Rect2(0, 0, MapWidth, MapHeight), borderColor, false, 4f);

        // 内部装饰边框
        float inset = 30;
        var innerColor = new Color(borderColor.R, borderColor.G, borderColor.B, 0.3f);
        DrawRect(new Rect2(inset, inset, MapWidth - inset * 2, MapHeight - inset * 2), innerColor, false, 1f);
    }
}
