using CommunityToolkit.Mvvm.ComponentModel;

namespace SongPrompter.Models
{
    public partial class Verse : ObservableObject
    {
        [ObservableProperty]
        private string name;
        [ObservableProperty]
        private int startBar;
        [ObservableProperty]
        private string lyrics;
    }
}
