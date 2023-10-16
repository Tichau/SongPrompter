using SongPrompter.ViewModels;
using System.Diagnostics;

using SongPrompter.Models;

namespace SongPrompter
{
    [QueryProperty(nameof(Playlist), SongPage.PlaylistParameterName)]
    public partial class SongPage : ContentPage
    {
        public const string PlaylistParameterName = "playlist";

        public SongPage(SongViewModel viewModel)
        {
            InitializeComponent();
            this.BindingContext = viewModel;
        }

        public Playlist Playlist
        {
            get => ((SongViewModel)this.BindingContext).Playlist;
            set
            {
                ((SongViewModel)this.BindingContext).Bind(value);
                OnPropertyChanged();
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            DeviceDisplay.Current.KeepScreenOn = true;
        }

        protected override void OnDisappearing()
        {
            DeviceDisplay.Current.KeepScreenOn = false;

            base.OnDisappearing();
        }
    }
}
