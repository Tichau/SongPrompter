using Godot;

namespace Core.UI;

public abstract partial class Panel : Control
{
    protected float FadeInDuration = 0.2f;
    protected float FadeOutDuration = 0f;

    private Tween? fadeTween;

    public override void _Ready()
    {
        this.Hide();
    }

    public override void _Process(double delta)
    {
        if (this.ShouldBeVisible())
        {
            if (this.fadeTween == null && !this.Visible)
            {
                this.ShowPanel();
            }
        }
        else
        {
            if (this.fadeTween == null && this.Visible)
            {
                this.HidePanel();
            }
        }
    }

    protected abstract bool ShouldBeVisible();

    protected virtual void ShowPanel()
    {
        this.Show();
        this.fadeTween = this.CreateTween();
        this.fadeTween.TweenProperty(this, "modulate", Colors.White, this.FadeInDuration)
            .From(Colors.Transparent)
            .SetTrans(Tween.TransitionType.Sine);
        this.fadeTween.Chain().TweenCallback(Callable.From(() => this.fadeTween = null));
    }

    protected virtual void HidePanel()
    {
        this.fadeTween = this.CreateTween();
        this.fadeTween.TweenProperty(this, "modulate", Colors.Transparent, this.FadeOutDuration)
            .From(Colors.White)
            .SetTrans(Tween.TransitionType.Sine);
        this.fadeTween.Chain().TweenCallback(Callable.From(() =>
        {
            this.Hide();
            this.fadeTween = null;
            this.OnPanelHidden();
        }));
    }

    protected virtual void OnPanelHidden()
    {
    }
}
