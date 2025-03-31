using System.Diagnostics;
using Application;
using Core.Extensions;
using Godot;

namespace UI;

public partial class SongList : Core.UI.Panel
{
    [Export] private PackedScene? songSelectorTemplate;
    [Export] private Label? setlistName;
    [Export] private Label? setlistInfo;
    [Export] private Container? setlistContent;

    private AppState? app;

    public override void _Ready()
    {
        base._Ready();

        this.app = App.Services.Get<AppState>();

        Debug.Assert(this.setlistContent != null);
        this.setlistContent.QueueFreeChildren();
    }

    protected override bool ShouldBeVisible()
    {
        Debug.Assert(this.app != null);
        return this.app.SongList && this.app.CurrentSetlist != null;
    }

    public void OnBackPressed()
    {
        Debug.Assert(this.app != null);
        this.app.SongList = false;
    }

    protected override void ShowPanel()
    {
        base.ShowPanel();
    
        Debug.Assert(this.setlistContent != null);
        this.setlistContent.QueueFreeChildren();

        Debug.Assert(this.setlistName != null && this.setlistInfo != null && this.songSelectorTemplate != null);
        Debug.Assert(this.app != null && this.app.CurrentSetlist != null);
        this.setlistName.Text = this.app.CurrentSetlist.Name;
        this.setlistInfo.Text = $"{this.app.CurrentSetlist.Songs.Length} songs";
        for (int songIndex = 0; songIndex < this.app.CurrentSetlist.Songs.Length; ++songIndex)
        {
            SongSelector song = this.songSelectorTemplate.Instantiate<SongSelector>();
            song.Bind(songIndex);
            this.setlistContent.AddChild(song);
        }
    }
}
