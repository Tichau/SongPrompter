using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SongPrompter.Models;
using SongPrompter.Services;

namespace SongPrompter.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        public IDataService dataService;

        public MainViewModel(IDataService dataService) 
        {
            this.DataService = dataService;
        }

        [RelayCommand]
        private void Play(object playlist)
        {
            Dictionary<string, object> data = new()
            {
                { SongPage.PlaylistParameterName, playlist }
            };

            Shell.Current.GoToAsync("song", data);
        }

        [RelayCommand]
        private void DeletePlaylist(object playlist)
        {
            this.DataService.RemovePlaylist((Playlist)playlist);
        }

        [RelayCommand]
        private async Task AddPlaylist()
        {
            try
            {
                CancellationToken cancellationToken = default;
                var result = await FolderPicker.Default.PickAsync(cancellationToken);
                if (result.IsSuccessful)
                {
                    this.DataService.LoadPlaylist(result.Folder.Path);
                }
            }
            catch (Exception exception)
            {
                // The user canceled or something went wrong
                Console.WriteLine(exception);
            }
        }
    }
}
