using Application;
using Godot;
using System.Diagnostics;

namespace UI;

public partial class Setlist : Control
{
    [Export] private Label? name;

    private Data.Setlist? setlist;

    public void Bind(Data.Setlist data)
    {
        this.setlist = data;

        Debug.Assert(this.name != null);
        this.name.Text = data.Name;
    }

    public void OnPlayPressed()
    {
        AppState app = App.Services.Get<AppState>();
        app.CurrentSetlist = this.setlist;
        app.CurrentSongIndex = 0;
    }

    public void OnRemovePressed()
    {
        Debug.Assert(this.setlist != null);
        App.Services.Get<SetlistDatabase>().RemoveSetlist(this.setlist);
    }
}
