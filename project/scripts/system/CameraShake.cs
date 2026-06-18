using Godot;

namespace Miao.System;

/// <summary>
/// 屏幕震动效果 — 挂在 Camera2D 上
/// 用法: GetNode&lt;CameraShake&gt;("Camera2D").Shake(4f, 0.15f);
/// </summary>
public partial class CameraShake : Camera2D
{
    private float _shakeIntensity = 0f;
    private float _shakeDuration = 0f;
    private float _shakeTimer = 0f;

    /// <summary>
    /// 触发屏幕震动
    /// </summary>
    /// <param name="intensity">震动强度（建议 2-15）</param>
    /// <param name="duration">持续时间（秒）</param>
    public void Shake(float intensity, float duration)
    {
        _shakeIntensity = intensity;
        _shakeDuration = duration;
        _shakeTimer = 0f;
    }

    public override void _Process(double delta)
    {
        if (_shakeTimer < _shakeDuration)
        {
            _shakeTimer += (float)delta;
            float progress = _shakeTimer / _shakeDuration;

            // 强度随时间衰减
            float currentIntensity = _shakeIntensity * (1f - progress);

            // 随机方向偏移
            float angle = GD.Randf() * Mathf.Pi * 2f;
            Offset = new Vector2(
                Mathf.Cos(angle) * currentIntensity,
                Mathf.Sin(angle) * currentIntensity
            );
        }
        else
        {
            Offset = Vector2.Zero;
        }
    }
}
