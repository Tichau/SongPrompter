namespace Data;

public partial class Song
{
    public string Name;
    public string Artist;
    
    public int Bpm;
    public int BeatPerBar;
    public int BeatSubdivision;
    
    public string Key;
    public string Transition;
    public Verse[] Verses;
}
