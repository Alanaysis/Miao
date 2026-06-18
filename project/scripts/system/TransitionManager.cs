using Godot;

namespace Miao.System;

/// <summary>
/// 场景过渡管理器 — 淡入淡出效果
/// 作为 CanvasLayer 挂在场景最高层
/// </summary>
public partial class TransitionManager : CanvasLayer
{
    private ColorRect _rect;
    private Tween _currentTween;

    public static TransitionManager Instance { get; private set; }

    public override void _Ready()
    {
        Instance = this;
        Layer = 100;

        _rect = new ColorRect();
        _rect.Color = new Color(0, 0, 0, 0);
        _rect.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _rect.MouseFilter = Control.MouseFilterEnum.Ignore;
        AddChild(_rect);
    }

    /// <summary>
    /// 淡入（从黑到透明）
    /// </summary>
    public void FadeIn(float duration = 0.3f)
    {
        _currentTween?.Kill();
        _currentTween = CreateTween();
        _currentTween.TweenProperty(_rect, "color:a", 0.0f, duration);
        _currentTween.SetTrans(Tween.TransitionType.Quad);
        _currentTween.SetEase(Tween.EaseType.Out);
    }

    /// <summary>
    /// 淡出（从透明到黑）
    /// </summary>
    public void FadeOut(float duration = 0.3f)
    {
        _currentTween?.Kill();
        _currentTween = CreateTween();
        _currentTween.TweenProperty(_rect, "color:a", 1.0f, duration);
        _currentTween.SetTrans(Tween.TransitionType.Quad);
        _currentTween.SetEase(Tween.EaseType.In);
    }

    /// <summary>
    /// 过渡到新场景（淡出 → 切换 → 淡入）
    /// </summary>
    public async void TransitionToScene(string scenePath, float fadeDuration = 0.3f)
    {
        FadeOut(fadeDuration);
        await ToSignal(_currentTween, Tween.SignalName.Finished);

        GetTree().ChangeSceneToFile(scenePath);

        // 等一帧确保场景加载
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        FadeIn(fadeDuration);
    }
}
