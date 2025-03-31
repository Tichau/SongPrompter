using Application;
using Godot;
using System.Diagnostics;

namespace UI;

public partial class SongSelector : Control
{
    [Export] private Label? index;
    [Export] private Label? name;

    private int songIndex;

    public void Bind(int songIndex)
    {
        this.songIndex = songIndex;

        Debug.Assert(this.name != null && this.index != null);
        AppState app = App.Services.Get<AppState>();
        Debug.Assert(app.CurrentSetlist != null);
        this.index.Text = (songIndex + 1).ToString("00");
        this.name.Text = app.CurrentSetlist.Songs[songIndex].Name;
    }

    public void OnPlayPressed()
    {
        AppState app = App.Services.Get<AppState>();
        app.CurrentSongIndex = this.songIndex;
        app.SongList = false;
    }
}
