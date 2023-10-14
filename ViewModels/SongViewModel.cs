using CommunityToolkit.Mvvm.ComponentModel;
using System.Diagnostics;

using SongPrompter.Models;
using System.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace SongPrompter.ViewModels
{
    public partial class SongViewModel : ObservableObject
    {
        private readonly Style defaultStyle;
        private readonly Style flashStyle;
        private readonly TimeSpan flashDuration = TimeSpan.FromMilliseconds(50);
        private readonly TimeSpan threadWakeupDuration = TimeSpan.FromMilliseconds(15);

        [ObservableProperty]
        private Playlist playlist;

        [ObservableProperty]
        private Song currentSong;

        [ObservableProperty]
        private string infos;

        [ObservableProperty]
        private string lyrics;

        [ObservableProperty]
        private Style metronomeStyle;

        [ObservableProperty]
        private int beat;

        public int nextSongIndex = 0;

        private readonly BackgroundWorker backgroundWorker;

        public SongViewModel()
        {
            if (App.Current.Resources.TryGetValue("LabelStyle", out var style))
            {
                this.defaultStyle = (Style)style;
            }

            if (App.Current.Resources.TryGetValue("MetronomeFlashStyle", out style))
            {
                this.flashStyle = (Style)style;
            }

            this.backgroundWorker = new BackgroundWorker
            {
                WorkerSupportsCancellation = true
            };

            this.backgroundWorker.DoWork += this.BackgroundWorker_DoWork;
        }

        internal void Bind(Playlist playlist)
        {
            this.Playlist = playlist;
            this.nextSongIndex = 0;
            this.LoadNextSong();
        }

        internal void LoadNextSong()
        {
            Debug.Assert(this.Playlist != null);
            if (this.nextSongIndex < this.Playlist.Songs.Length)
            {
                this.backgroundWorker.CancelAsync();
                while (this.backgroundWorker.IsBusy)
                {
                    Thread.Sleep(1);
                }

                this.CurrentSong = this.Playlist.Songs[this.nextSongIndex];
                this.Infos = $"{this.CurrentSong.Bpm} bpm    Signature: {this.CurrentSong.BeatPerMeasure}/{this.CurrentSong.BeatSubdivision}    Key: {this.CurrentSong.Key}";
                this.Lyrics = this.CurrentSong.Lyrics.Aggregate((left, right) => left + '\n' + right);
                this.nextSongIndex++;

                this.backgroundWorker.RunWorkerAsync();
            }
        }

        private void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            Stopwatch stopwatch = Stopwatch.StartNew();

            TimeSpan timeElapsedUntilLastBeat = TimeSpan.Zero;

            this.Beat = 0;
            TimeSpan beatDuration = TimeSpan.FromMinutes(1.0 / this.CurrentSong.Bpm);
            while (!worker.CancellationPending)
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

                    this.MetronomeStyle = this.flashStyle;
                    this.Beat++;

                    Thread.Sleep(this.flashDuration);

                    this.MetronomeStyle = this.defaultStyle;

                    //if (this.scroll)
                    //{
                    //    Dispatcher.Dispatch(scrollAction);
                    //}
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

        [RelayCommand]
        private async void Start()
        {
            this.Beat = 0;
        }
    }
}

