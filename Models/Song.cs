using CommunityToolkit.Mvvm.ComponentModel;

namespace SongPrompter.Models
{
    public partial class Song : ObservableObject
    {
        [ObservableProperty]
        private string name;
        [ObservableProperty]
        private string artist;
        
        [ObservableProperty]
        private int bpm;
        [ObservableProperty]
        private int beatPerBar;
        [ObservableProperty]
        private int beatSubdivision;
        
        [ObservableProperty]
        private string key;

        [ObservableProperty]
        private Verse[] verses;
    }
}
