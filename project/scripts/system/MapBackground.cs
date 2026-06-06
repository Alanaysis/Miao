using Godot;

namespace Miao.System;

/// <summary>
/// 地图背景
/// 绘制网格线和装饰物，给玩家提供移动参照
/// </summary>
public partial class MapBackground : Node2D
{
    /// <summary>网格大小</summary>
    [Export] public int GridSize = 100;

    /// <summary>地图宽度</summary>
    [Export] public float MapWidth = 2000f;

    /// <summary>地图高度</summary>
    [Export] public float MapHeight = 2000f;

    /// <summary>装饰物数量</summary>
    [Export] public int DecorationCount = 60;

    private Vector2[] _decorations;
    private Color[] _decorationColors;

    public override void _Ready()
    {
        ZIndex = -10;
        GenerateDecorations();
    }

    private void GenerateDecorations()
    {
        _decorations = new Vector2[DecorationCount];
        _decorationColors = new Color[DecorationCount];

        var rng = new RandomNumberGenerator();
        rng.Randomize();

        for (int i = 0; i < DecorationCount; i++)
        {
            _decorations[i] = new Vector2(
                rng.RandfRange(0, MapWidth),
                rng.RandfRange(0, MapHeight)
            );

            // 随机灰绿色调（草地/石头）
            float g = rng.RandfRange(0.15f, 0.3f);
            _decorationColors[i] = new Color(g * 0.7f, g, g * 0.5f, 0.6f);
        }
    }

    public override void _Draw()
    {
        // 画网格线
        var gridColor = new Color(0.2f, 0.25f, 0.2f, 0.3f);

        for (float x = 0; x <= MapWidth; x += GridSize)
        {
            DrawLine(new Vector2(x, 0), new Vector2(x, MapHeight), gridColor, 1f);
        }
        for (float y = 0; y <= MapHeight; y += GridSize)
        {
            DrawLine(new Vector2(0, y), new Vector2(MapWidth, y), gridColor, 1f);
        }

        // 画装饰物（随机大小的圆形）
        if (_decorations == null) return;
        for (int i = 0; i < _decorations.Length; i++)
        {
            float radius = 8f + (i % 5) * 4f;
            DrawCircle(_decorations[i], radius, _decorationColors[i]);
        }

        // 画地图边界
        var borderColor = new Color(0.4f, 0.3f, 0.2f, 0.8f);
        DrawRect(new Rect2(0, 0, MapWidth, MapHeight), borderColor, false, 4f);
    }
}
