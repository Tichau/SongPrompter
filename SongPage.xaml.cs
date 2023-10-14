using SongPrompter.ViewModels;
using System.Diagnostics;

using SongPrompter.Models;

namespace SongPrompter
{
    [QueryProperty(nameof(Playlist), SongPage.PlaylistParameterName)]
    public partial class SongPage : ContentPage
    {
        public const string PlaylistParameterName = "playlist";

        private readonly Color defaultColor = new Color(1f);
        private readonly Color flashColor = new Color(0.5f);

        private readonly TimeSpan flashDuration = TimeSpan.FromMilliseconds(50);
        private readonly TimeSpan threadWakeupDuration = TimeSpan.FromMilliseconds(15);

        private bool scroll = false;

        private volatile bool exitUpdate;
        private Task updateTask;

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
                if (this.updateTask != null)
                {
                    this.exitUpdate = true;
                    this.updateTask.Wait();
                }

                ((SongViewModel)this.BindingContext).Bind(value);
                
                this.updateTask = Task.Run(this.Update);

                OnPropertyChanged();
            }
        }

        private void Update()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            Action flashAction = () => this.metronome.BackgroundColor = this.flashColor;
            Action resetAction = () => this.metronome.BackgroundColor = this.defaultColor;
            Action scrollAction = () => this.scrollView.ScrollToAsync(0, this.scrollView.ScrollY + 10, true);

            TimeSpan timeElapsedUntilLastBeat = TimeSpan.Zero;

            Song currentSong = ((SongViewModel)this.BindingContext).CurrentSong;
            if (currentSong == null)
            {
                return;
            }

            TimeSpan beatDuration = TimeSpan.FromMinutes(1.0 / currentSong.Bpm);
            while (!this.exitUpdate)
            {
                var timeElapsed = stopwatch.Elapsed;
                var durationSinceLastBeat = timeElapsed - timeElapsedUntilLastBeat;
                if (durationSinceLastBeat >= beatDuration) 
                {
                    var lateness = durationSinceLastBeat - beatDuration;

                    timeElapsedUntilLastBeat = timeElapsed;
                    if (lateness < beatDuration)
                    {
                        // Fix up latencies.
                        timeElapsedUntilLastBeat -= lateness;
                    }

                    Dispatcher.Dispatch(flashAction);
                    Thread.Sleep(this.flashDuration);
                    Dispatcher.Dispatch(resetAction);

                    if (this.scroll)
                    {
                        Dispatcher.Dispatch(scrollAction);
                    }
                }
                else
                {
                    var durationFromNextBeat = beatDuration - durationSinceLastBeat - this.threadWakeupDuration;
                    if (durationFromNextBeat > TimeSpan.Zero)
                    {
                        Thread.Sleep(durationFromNextBeat);
                    }
                }
            }
        }

        private void StartButton_Clicked(object sender, EventArgs e)
        {
            //this.scrollView.ScrollToAsync(this.lyrics, ScrollToPosition.Center, true);
            this.scroll = true;
        }
    }
}
