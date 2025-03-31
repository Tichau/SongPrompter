
using Godot;

namespace Core.Extensions;

public static class TweenExtensions
{
    public static PropertyTweener TweenFadeInAndPlay(this Tween tween, AudioStreamPlayer streamPlayer, float duration = 0.2f, float fromPosition = 0f, float targetVolume = 0)
    {
        tween.TweenCallback(Callable.From(() => streamPlayer.Play(fromPosition)));
        PropertyTweener propertyTweener = tween.Parallel().TweenProperty(streamPlayer, "volume_db", targetVolume, duration)
            .From(-80);
        return propertyTweener;
    }

    public static PropertyTweener TweenFadeOutAndStop(this Tween tween, AudioStreamPlayer streamPlayer, float duration = 1f)
    {
        PropertyTweener propertyTweener = tween.TweenProperty(streamPlayer, "volume_db", -80, duration)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);
        tween.TweenCallback(Callable.From(streamPlayer.Stop));
        return propertyTweener;
    }

    public static PropertyTweener TweenFadeOutAndStop(this Tween tween, VideoStreamPlayer streamPlayer, float duration = 1f)
    {
        PropertyTweener propertyTweener = tween.TweenProperty(streamPlayer, "volume_db", -80, duration)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);
        tween.TweenCallback(Callable.From(streamPlayer.Stop));
        return propertyTweener;
    }

    public static PropertyTweener TweenFadeOut(this Tween tween, Control control, float duration = 1f)
    {
        PropertyTweener propertyTweener = tween.TweenProperty(control, "modulate", UI.Colors.Transparent, duration)
            .SetTrans(Tween.TransitionType.Sine);
        return propertyTweener;
    }

    public static PropertyTweener TweenFadeIn(this Tween tween, Control control, float duration = 1f)
    {
        PropertyTweener propertyTweener = tween.TweenProperty(control, "modulate", UI.Colors.White, duration)
            .SetTrans(Tween.TransitionType.Sine);
        return propertyTweener;
    }
}
