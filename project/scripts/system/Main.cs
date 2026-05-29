using Godot;

namespace Miao.System;

/// <summary>
/// 主场景脚本
/// 游戏入口点，负责初始化游戏系统
/// </summary>
public partial class Main : Node2D
{
    public override void _Ready()
    {
        // 游戏初始化
        GD.Print("游戏启动！");
        GD.Print("当前分支: experiment/roguelike");
        GD.Print("这是一个Roguelike类型的实验 (C#)");
    }
}
