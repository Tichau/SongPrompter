using SongPrompter.ViewModels;

using SongPrompter.Models;

namespace SongPrompter
{
    [QueryProperty(nameof(Playlist), SongPage.PlaylistParameterName)]
    public partial class SongPage : ContentPage
    {
        public const string PlaylistParameterName = "playlist";

        public double[] yVersesPositions;

        private SongViewModel ViewModel
        {
            get => this.BindingContext as SongViewModel;
        }

        public SongPage(SongViewModel viewModel)
        {
            InitializeComponent();
            this.BindingContext = viewModel;

            viewModel.VerseChanged += this.ViewModel_VerseChanged;
            viewModel.SongStarted += this.ViewModel_SongStarted;
        }

        public Playlist Playlist
        {
            get => ((SongViewModel)this.BindingContext).Playlist;
            set
            {
                this.ViewModel.Bind(value);
                if (this.ViewModel.CurrentSong.BeatSubdivision != 4 || this.ViewModel.CurrentSong.BeatPerBar <= 0)
                {
                    DisplayAlert("Warning", $"Unsupported time signature: {this.ViewModel.CurrentSong.BeatPerBar}/{this.ViewModel.CurrentSong.BeatSubdivision}", "OK");
                }

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

        private void ViewModel_SongStarted(Song newSong)
        {
            // Compute Y position for each verse.
            VisualElement[] elements = this.verses.GetVisualTreeDescendants().Where(element => element is VerticalStackLayout).Cast<VisualElement>().ToArray();
            this.yVersesPositions = new double[newSong.Verses.Length];

            VisualElement element = elements[0];
            this.yVersesPositions[0] = 0.0;
            while (element != null)
            {
                this.yVersesPositions[0] += element.Y;
                element = element.Parent as VisualElement;
            }

            for (int index = 1; index < this.yVersesPositions.Length; index++)
            {
                this.yVersesPositions[index] = this.yVersesPositions[index - 1] + elements[index - 1].Height;
            }
        }

        private void ViewModel_VerseChanged(int newVerseId)
        {
            this.Dispatcher.Dispatch(() => this.scrollView.ScrollToAsync(0.0, this.yVersesPositions[newVerseId], true));
        }
    }
}
