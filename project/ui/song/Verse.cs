using Godot;
using System.Diagnostics;

namespace UI;

public partial class Verse : Control
{
    [Export] private RichTextLabel? info;
    [Export] private Label? lyrics;

    public Data.Verse? Data
    {
        get;
        private set;
    }

    public void Bind(Data.Verse verse)
    {
        Debug.Assert(this.info != null && this.lyrics != null);
        this.info.Text = string.Format(this.info.Text, verse.Name, verse.StartBar);
        this.lyrics.Text = verse.Lyrics;
        this.Data = verse;
    }
}
