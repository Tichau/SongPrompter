using System.Diagnostics;
using Application;
using Core.Extensions;
using Godot;

namespace UI;

public partial class MainMenu : Core.UI.Panel
{
    [Export] private Container? setlistContainer;
    [Export] private FileDialog? selectSetlistDialog;
    [Export] private PackedScene? setlistTemplate;

    private AppState? app;
    private SetlistDatabase? setlistDatabase;

    public override void _Ready()
    {
        base._Ready();

        Debug.Assert(this.setlistContainer != null);
        this.setlistContainer.QueueFreeChildren();

        this.app = App.Services.Get<AppState>();
        this.setlistDatabase = App.Services.Get<SetlistDatabase>();

        Debug.Assert(this.selectSetlistDialog != null);
        this.selectSetlistDialog.DirSelected += this.OnFileDialogConfirmed;

        Engine.MaxFps = 30;
    }

    protected override bool ShouldBeVisible()
    {
        Debug.Assert(this.app != null);
        return this.app.CurrentSetlist == null;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (!this.Visible)
        {
            return;
        }

        Debug.Assert(this.setlistContainer != null && this.setlistDatabase != null && this.setlistTemplate != null);
        this.setlistContainer.MapChildren(() => this.setlistTemplate.Instantiate<Setlist>(), this.setlistDatabase.Data, (game, ui) => ui.Bind(game));
    }

    public void OnAddPlaylistPressed()
    {
        if (OS.RequestPermissions()) 
        {
            Debug.Assert(this.selectSetlistDialog != null);
            this.selectSetlistDialog.Show();
        }
    }

    public void OnFileDialogConfirmed(string folder)
    {
        Debug.Assert(this.setlistDatabase != null);
        this.setlistDatabase.LoadSetlist(folder);
    }
}
