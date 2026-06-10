using Godot;

namespace Miao.System;

/// <summary>
/// 地图边界
/// 限制玩家和敌人在指定区域内移动
/// </summary>
public partial class MapBoundary : Node2D
{
    [Export] public float MapWidth = 2000f;
    [Export] public float MapHeight = 2000f;
    [Export] public float BoundaryThickness = 50f;

    private ColorRect[] _wallVisuals;

    private static readonly Color[] WallColors = new Color[]
    {
        new Color(0.15f, 0.25f, 0.12f, 0.7f), // 草地
        new Color(0.28f, 0.22f, 0.12f, 0.7f), // 荒地
        new Color(0.12f, 0.12f, 0.22f, 0.7f), // 洞穴
        new Color(0.28f, 0.1f, 0.15f, 0.7f),  // 虫巢
        new Color(0.15f, 0.1f, 0.28f, 0.7f),  // 深渊
    };

    // 地图主题边框颜色
    private static readonly (string theme, Color color)[] MapWallColors = new[]
    {
        ("hive",      new Color(0.2f, 0.35f, 0.1f, 0.7f)),
        ("wasteland",  new Color(0.35f, 0.3f, 0.2f, 0.7f)),
        ("cave",       new Color(0.2f, 0.2f, 0.3f, 0.7f)),
        ("void",       new Color(0.25f, 0.12f, 0.4f, 0.7f)),
        ("solar",      new Color(0.5f, 0.2f, 0.08f, 0.7f)),
    };

    private string _currentMapTheme = "hive";

    public override void _Ready()
    {
        _wallVisuals = new ColorRect[4];
        CreateBoundaries();
    }

    public void SetRoomTheme(int roomIndex)
    {
        int idx = Mathf.Clamp(roomIndex, 0, WallColors.Length - 1);
        var color = WallColors[idx];
        foreach (var v in _wallVisuals)
        {
            if (v != null) v.Color = color;
        }
    }

    /// <summary>
    /// 设置地图主题边框颜色
    /// </summary>
    public void SetMapTheme(string themeId)
    {
        _currentMapTheme = themeId ?? "hive";
        var color = GetMapWallColor();
        foreach (var v in _wallVisuals)
        {
            if (v != null) v.Color = color;
        }
    }

    private Color GetMapWallColor()
    {
        foreach (var (theme, color) in MapWallColors)
        {
            if (theme == _currentMapTheme) return color;
        }
        return MapWallColors[0].color; // Default hive
    }

    private void CreateBoundaries()
    {
        var positions = new Vector2[]
        {
            new Vector2(MapWidth / 2, -BoundaryThickness / 2),
            new Vector2(MapWidth / 2, MapHeight + BoundaryThickness / 2),
            new Vector2(-BoundaryThickness / 2, MapHeight / 2),
            new Vector2(MapWidth + BoundaryThickness / 2, MapHeight / 2),
        };
        var sizes = new Vector2[]
        {
            new Vector2(MapWidth + BoundaryThickness * 2, BoundaryThickness),
            new Vector2(MapWidth + BoundaryThickness * 2, BoundaryThickness),
            new Vector2(BoundaryThickness, MapHeight),
            new Vector2(BoundaryThickness, MapHeight),
        };

        for (int i = 0; i < 4; i++)
        {
            var wall = new StaticBody2D();
            wall.Position = positions[i];
            wall.CollisionLayer = 8;

            var shape = new RectangleShape2D();
            shape.Size = sizes[i];

            var collision = new CollisionShape2D();
            collision.Shape = shape;
            wall.AddChild(collision);

            var visual = new ColorRect();
            visual.Color = WallColors[0];
            visual.Size = sizes[i];
            visual.Position = -sizes[i] / 2;
            wall.AddChild(visual);

            _wallVisuals[i] = visual;
            AddChild(wall);
        }
    }
}
