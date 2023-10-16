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
        private Style metronomeStyle;

        [ObservableProperty]
        private int beat;
        [ObservableProperty]
        private int measure;

        private bool started = false;
        private int currentVerseIndex;

        public int nextSongIndex = 0;

        private readonly BackgroundWorker backgroundWorker;

        public delegate void SongStartedEventHandler(Song newSong);
        public delegate void VerseChangedEventHandler(int newVerseId);

        public event VerseChangedEventHandler VerseChanged;
        public event SongStartedEventHandler SongStarted;

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

            this.CurrentSong = new Song()
            {
                Verses = new Verse[0]
            };
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

                    if (this.started)
                    {
                        this.Beat++;
                        this.Measure = this.Beat / this.CurrentSong.BeatPerMeasure;

                        if (this.CurrentSong.Verses.Length > currentVerseIndex + 1 &&
                            this.Measure >= this.CurrentSong.Verses[currentVerseIndex + 1].StartMeasure)
                        {
                            currentVerseIndex++;
                            this.VerseChanged?.Invoke(currentVerseIndex);
                        }
                    }

                    Thread.Sleep(this.flashDuration);

                    this.MetronomeStyle = this.defaultStyle;
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
            this.started = true;
            this.Beat = 0;
            this.Measure = 0;
            this.currentVerseIndex = -1;

            this.SongStarted?.Invoke(this.CurrentSong);
        }
    }
}

