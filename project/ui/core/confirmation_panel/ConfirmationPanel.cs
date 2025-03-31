using System.Diagnostics;
using Core.Extensions;
using Godot;

namespace Core.UI;

public partial class ConfirmationPanel : Control
{
    [Export] private Label? titlePanel;
    [Export] private RichTextLabel? dialogText;
    [Export] private Button? okButton;
    [Export] private Button? cancelButton;

    private System.Action? confirmed;
    private System.Action? canceled;

    private static ConfirmationPanel? instance;

    public ConfirmationPanel()
    {
        Debug.Assert(ConfirmationPanel.instance == null, "There should have only one confirmation panel.");
        ConfirmationPanel.instance = this;

        this.Hide();
    }

    public override void _UnhandledInput(InputEvent inputEvent)
    {
        if (!this.Visible)
        {
            return;
        }

        // Absorb all inputs.
        this.GetViewport().SetInputAsHandled();
        if (inputEvent.IsActionReleased("action"))
        {
            this.OnOkPressed();
        }
        else if (inputEvent.IsActionReleased("back"))
        {
            this.OnCanceledPressed();
        }
    }

    public static void ShowConfirmation(string title, string dialogText, string okButtonText, string cancelButtonText, System.Action confirmed, System.Action? canceled = null)
    {
        Debug.Assert(ConfirmationPanel.instance != null);
        ConfirmationPanel panel = ConfirmationPanel.instance;

        Debug.Assert(panel.titlePanel != null);
        panel.titlePanel.Text = title;
        Debug.Assert(panel.dialogText != null);
        panel.dialogText.Text = dialogText;
        Debug.Assert(panel.okButton != null);
        panel.okButton.Text = okButtonText;
        Debug.Assert(panel.cancelButton != null);
        panel.cancelButton.Visible = true;
        panel.cancelButton.Text = cancelButtonText;
        panel.confirmed = confirmed;
        panel.canceled = canceled;

        Tween tween = panel.CreateTween();
        tween.TweenCallback(Callable.From(panel.Show));
        tween.TweenFadeIn(panel, duration: 0.3f);
    }

    public static void ShowInformation(string title, string dialogText, string okButtonText)
    {
        Debug.Assert(ConfirmationPanel.instance != null);
        ConfirmationPanel panel = ConfirmationPanel.instance;

        Debug.Assert(panel.titlePanel != null);
        panel.titlePanel.Text = title;
        Debug.Assert(panel.dialogText != null);
        panel.dialogText.Text = dialogText;
        Debug.Assert(panel.okButton != null);
        panel.okButton.Text = okButtonText;

        Debug.Assert(panel.cancelButton != null);
        panel.cancelButton.Visible = false;

        Tween tween = panel.CreateTween();
        tween.TweenCallback(Callable.From(panel.Show));
        tween.TweenFadeIn(panel, duration: 0.3f);
    }

    public void HideConfirmation()
    {
        this.confirmed = null;
        this.canceled = null;

        Tween tween = this.CreateTween();
        tween.TweenFadeOut(this, duration: 0.3f);
        tween.TweenCallback(Callable.From(this.Hide));
    }

    private void OnOkPressed()
    {
        this.confirmed?.Invoke();

        this.HideConfirmation();
    }

    private void OnCanceledPressed()
    {
        this.canceled?.Invoke();

        this.HideConfirmation();
    }
}
