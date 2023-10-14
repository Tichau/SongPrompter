using CommunityToolkit.Mvvm.ComponentModel;

namespace SongPrompter.Models
{
    public partial class Playlist : ObservableObject
    {
        [ObservableProperty]
        private string name;
        [ObservableProperty]
        private string path;
        [ObservableProperty]
        private Song[] songs;
    }
}
