using Godot;

namespace Miao.System;

/// <summary>
/// 地图边界
/// 限制玩家和敌人在指定区域内移动
/// </summary>
public partial class MapBoundary : Node2D
{
    /// <summary>地图宽度</summary>
    [Export] public float MapWidth = 2000f;

    /// <summary>地图高度</summary>
    [Export] public float MapHeight = 2000f;

    /// <summary>边界厚度</summary>
    [Export] public float BoundaryThickness = 50f;

    public override void _Ready()
    {
        CreateBoundaries();
    }

    private void CreateBoundaries()
    {
        // 上边界
        CreateBoundaryWall(
            new Vector2(MapWidth / 2, -BoundaryThickness / 2),
            new Vector2(MapWidth + BoundaryThickness * 2, BoundaryThickness)
        );

        // 下边界
        CreateBoundaryWall(
            new Vector2(MapWidth / 2, MapHeight + BoundaryThickness / 2),
            new Vector2(MapWidth + BoundaryThickness * 2, BoundaryThickness)
        );

        // 左边界
        CreateBoundaryWall(
            new Vector2(-BoundaryThickness / 2, MapHeight / 2),
            new Vector2(BoundaryThickness, MapHeight)
        );

        // 右边界
        CreateBoundaryWall(
            new Vector2(MapWidth + BoundaryThickness / 2, MapHeight / 2),
            new Vector2(BoundaryThickness, MapHeight)
        );
    }

    private void CreateBoundaryWall(Vector2 position, Vector2 size)
    {
        var wall = new StaticBody2D();
        wall.Position = position;
        wall.CollisionLayer = 8; // 边界层

        var shape = new RectangleShape2D();
        shape.Size = size;

        var collision = new CollisionShape2D();
        collision.Shape = shape;
        wall.AddChild(collision);

        // 可视化边界（灰色）
        var visual = new ColorRect();
        visual.Color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        visual.Size = size;
        visual.Position = -size / 2;
        wall.AddChild(visual);

        AddChild(wall);
    }
}
