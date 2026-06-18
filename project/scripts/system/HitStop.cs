using Godot;

namespace Miao.System;

/// <summary>
/// 停顿效果（Hitstop）— 让重击有重量感
/// 用法: HitStop.Apply(GetTree(), 0.05f);
/// </summary>
public static class HitStop
{
    private static float _originalTimeScale = 1.0f;

    /// <summary>
    /// 触发停顿
    /// </summary>
    /// <param name="tree">SceneTree</param>
    /// <param name="duration">停顿实际时长（秒，不受 TimeScale 影响）</param>
    /// <param name="timeScale">停顿时的时间缩放（0.05 = 5%速度）</param>
    public static async void Apply(SceneTree tree, float duration = 0.05f, float timeScale = 0.05f)
    {
        if (Engine.TimeScale != 1.0f) return; // 避免叠加

        _originalTimeScale = (float)Engine.TimeScale;
        Engine.TimeScale = timeScale;

        // 用 CreateTimer 等待，process_always=true 确保在 TimeScale 很低时也能触发
        await tree.ToSignal(
            tree.CreateTimer((double)duration, true, false, true),
            SceneTreeTimer.SignalName.Timeout
        );

        Engine.TimeScale = _originalTimeScale;
    }
}
