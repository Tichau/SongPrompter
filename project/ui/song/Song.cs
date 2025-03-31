using Application;
using Core.Extensions;
using Godot;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;

namespace UI;

public partial class Song : Core.UI.Panel
{
    [Export] private Control? swipeContainer;
    [Export] private ScrollContainer? scrollContainer;
    [Export] private Button? playButton;
    [Export] private Label? title;
    [Export] private Label? artist;
    [Export] private Label? bpm;
    [Export] private Label? signature;
    [Export] private Label? key;
    [Export] private Label? songNumber;
    [Export] private Godot.Container? verseContainer;
    [Export] private PackedScene? verseTemplate;
    
    [ExportSubgroup("Beat Feedback")]
    [Export] private ColorRect? beatFeedback;
    [Export] private Color defaultColor;
    [Export] private Color flashColor;
    [Export] private Color flashBarColor;
    [Export] private int flashDurationInMsec;
    [Export] private Label? barNumber;

    private AppState? app;
    private int songIndex = -1;

    private readonly AutoResetEvent resetEvent = new(false);
    private readonly TimeSpan threadWakeupDuration = TimeSpan.FromMilliseconds(15);
    private BackgroundWorker? backgroundWorker;

    private int beat;
    private bool started;
    private int bar;
    private int lastBar;
    private int currentVerseIndex;
    private BeatFeedbackState beatFeedbackState = BeatFeedbackState.Default;
    
    private SwipeMode swipeMode;
    private Vector2 swipeStartPosition;
    private Vector2 swipeDelta;
    private Vector2 swipeContainerDefaultPosition;

    private const int countInBar = 3;
    private const float swipeDeadZoneSquared = 10 * 10;
    private const float horizontalSwipeClampLength = 20;
    private const float horizontalSwipeLength = 100;

    public override void _Ready()
    {
        base._Ready();

        Debug.Assert(this.verseContainer != null);
        this.verseContainer.QueueFreeChildren();

        this.app = App.Services.Get<AppState>();

        Debug.Assert(this.barNumber != null);
        this.barNumber.Visible = false;
    }

    protected override bool ShouldBeVisible()
    {
        Debug.Assert(this.app != null);
        return this.app.CurrentSetlist != null;
    }

    protected override void ShowPanel()
    {
        this.songIndex = -1;

        base.ShowPanel();

        this.CreateTween().TweenCallback(Callable.From(() => {
            Debug.Assert(this.swipeContainer != null);
            this.swipeContainerDefaultPosition = this.swipeContainer.Position;
            Trace.WriteLine(this.swipeContainerDefaultPosition);
        })).SetDelay(0.01f);
    }

    protected override void OnPanelHidden()
    {
        this.StopMetronome();

        base.OnPanelHidden();
    }

    public void OnBackPressed()
    {
        Debug.Assert(this.app != null);
        this.app.CurrentSetlist = null;
    }

    public override void _UnhandledInput(InputEvent inputEvent)
    {
        bool CanSwipeLeft()
        {
            Debug.Assert(this.app != null);
            return this.app.CurrentSongIndex > 0;
        }

        bool CanSwipeRight()
        {
            Debug.Assert(this.app != null && this.app.CurrentSetlist != null);
            return this.app.CurrentSongIndex < this.app.CurrentSetlist.Songs.Length - 1;
        }

        bool touch = false;
        if (inputEvent is InputEventScreenTouch inputEventScreenTouch)
        {
            if (inputEvent.IsPressed())
            {
                this.swipeStartPosition = inputEventScreenTouch.Position;
                this.swipeDelta = Vector2.Zero;
                this.swipeMode = SwipeMode.None;
            }
            else
            {
                // Swipe ended
                if (swipeMode == SwipeMode.Horizontal)
                {
                    Debug.Assert(this.app != null && this.app.CurrentSetlist != null);
                    if (swipeDelta.X <= -horizontalSwipeLength && CanSwipeRight())
                    {
                        this.app.CurrentSongIndex++;
                    }
                    else if (swipeDelta.X >= horizontalSwipeLength && CanSwipeLeft())
                    {
                        this.app.CurrentSongIndex--;
                    }

                    this.CreateTween().TweenProperty(this.swipeContainer, "position:x", this.swipeContainerDefaultPosition.X, duration: 0.2f)
                        .SetTrans(Tween.TransitionType.Cubic);
                }
            }

            touch = true;
        }
        else if (inputEvent is InputEventScreenDrag inputEventScreenDrag)
        {
            this.swipeDelta = inputEventScreenDrag.Position - this.swipeStartPosition;
            touch = true;
        }

        if (touch && this.swipeDelta.LengthSquared() > swipeDeadZoneSquared)
        {
            if (this.swipeMode == SwipeMode.None)
            {
                this.swipeMode = Math.Abs(this.swipeDelta.X) > Math.Abs(this.swipeDelta.Y) ? SwipeMode.Horizontal : SwipeMode.Vertical;
            }

            switch (this.swipeMode)
            {
                case SwipeMode.Vertical:
                    Debug.Assert(this.scrollContainer != null);
                    this.scrollContainer.MouseFilter = MouseFilterEnum.Pass;
                    break;

                case SwipeMode.Horizontal:
                    if (!CanSwipeLeft())
                    {
                        this.swipeDelta.X = Math.Min(this.swipeDelta.X, horizontalSwipeClampLength);
                    }

                    if (!CanSwipeRight())
                    {
                        this.swipeDelta.X = Math.Max(this.swipeDelta.X, -horizontalSwipeClampLength);
                    }

                    Debug.Assert(this.swipeContainer != null && this.scrollContainer != null);
                    this.scrollContainer.MouseFilter = MouseFilterEnum.Ignore;
                    this.swipeContainer.Position = new Vector2(this.swipeContainerDefaultPosition.X + this.swipeDelta.X, this.swipeContainer.Position.Y);
                    this.AcceptEvent();
                    break;
            }
        }
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (!this.Visible)
        {
            return;
        }

        Debug.Assert(this.app != null);
        if (this.app.CurrentSetlist != null && this.songIndex != this.app.CurrentSongIndex)
        {
            this.songIndex = this.app.CurrentSongIndex;
            Data.Song song = this.app.CurrentSetlist.Songs[this.songIndex];

            Debug.Assert(this.title != null && this.artist != null && this.bpm != null && this.signature != null && this.key != null && this.songNumber != null && this.verseContainer != null && this.verseTemplate != null);
            this.title.Text = song.Name;
            this.artist.Text = song.Artist;
            this.bpm.Text = $"{song.Bpm} bpm";
            this.signature.Text = $"Signature: {song.BeatPerBar}/{song.BeatSubdivision}";
            this.key.Text = $"Key: {song.Key}";
            this.songNumber.Text = $"Song: {this.songIndex + 1}/{this.app.CurrentSetlist.Songs.Length}";

            this.verseContainer.QueueFreeChildren();
            this.verseContainer.MapChildren(() => this.verseTemplate.Instantiate<Verse>(), song.Verses, (game, ui) => ui.Bind(game));

            // TimeSpan beatDuration = TimeSpan.FromMinutes(1.0 / song.Bpm);
            // Engine.MaxFps = Mathf.CeilToInt(beatDuration.TotalSeconds * 20); 
            // GD.Print($"Set max FPS to {Engine.MaxFps}");
            this.StartMetronome();
        }

        // Update beat feedback.
        Debug.Assert(this.beatFeedback != null);
        switch (this.beatFeedbackState)
        {
            case BeatFeedbackState.Default:
                this.beatFeedback.Color = this.defaultColor;
                break;
            case BeatFeedbackState.Beat:
                this.beatFeedback.Color = this.flashColor;
                break;
            case BeatFeedbackState.Flash:
                this.beatFeedback.Color = this.flashBarColor;
                break;
        }

        if (this.bar != this.lastBar)
        {
            Debug.Assert(this.barNumber != null);
            this.barNumber.Text = this.bar.ToString();

            Debug.Assert(this.verseContainer != null && this.scrollContainer != null);
            foreach (var child in this.verseContainer.GetChildren())
            {
                var verse = child as Verse;
                Debug.Assert(verse != null && verse.Data != null);
                if (verse.Data.StartBar == this.bar)
                {
                    this.CreateTween().TweenProperty(this.scrollContainer, "scroll_vertical", Mathf.FloorToInt(verse.Position.Y + this.verseContainer.Position.Y), duration: 0.6f);
                    break;
                }
            }

            this.lastBar = this.bar;
        }
    }

    private void StartStop()
    {
        this.beat = -1;
        this.bar = this.lastBar = -Song.countInBar;
        this.currentVerseIndex = -1;

        Debug.Assert(this.playButton != null);
        Debug.Assert(this.barNumber != null);
        if (this.started)
        {
            this.started = false;
            this.playButton.Text = "Start";

            this.barNumber.Visible = false;
        }
        else
        {
            this.started = true;
            this.playButton.Text = "Stop";

            this.barNumber.Text = this.bar.ToString();
            this.barNumber.Visible = true;
        }
    }

    private void StartMetronome()
    {
        this.StopMetronome(waitForStop: true);
        if (this.started)
        {
            this.StartStop();
        }

        this.backgroundWorker = new BackgroundWorker
        {
            WorkerSupportsCancellation = true
        };

        this.backgroundWorker.DoWork += this.BackgroundWorker_DoWork;
        this.backgroundWorker.RunWorkerAsync();
    }

    private void StopMetronome(bool waitForStop = false)
    {
        if (this.backgroundWorker != null && this.backgroundWorker.IsBusy)
        {
            this.backgroundWorker.CancelAsync();
            if (waitForStop)
            {
                this.resetEvent.WaitOne();
            }
        }
    }

    private void BackgroundWorker_DoWork(object? sender, DoWorkEventArgs e)
    {
        BackgroundWorker? worker = sender as BackgroundWorker;
        Debug.Assert(worker != null);

        Stopwatch stopwatch = Stopwatch.StartNew();

        TimeSpan timeElapsedUntilLastBeat = TimeSpan.Zero;

        Debug.Assert(this.app != null && this.app.CurrentSetlist != null);
        Data.Song song = this.app.CurrentSetlist.Songs[this.songIndex];

        this.beat = 0;
        TimeSpan beatDuration = TimeSpan.FromMinutes(1.0 / song.Bpm);
        while (true)
        {
            if (worker.CancellationPending)
            {
                e.Cancel = true;
                break;
            }

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

                if (this.started)
                {
                    this.beat++;
                    int bar = this.beat / song.BeatPerBar;
                    this.bar = bar - Song.countInBar + 1;
                    if (this.bar <= 0)
                    {
                        this.bar--;
                    }

                    if (song.Verses.Length > this.currentVerseIndex + 1 &&
                        this.bar >= song.Verses[this.currentVerseIndex + 1].StartBar)
                    {
                        this.currentVerseIndex++;
                    }
                }

                Debug.Assert(this.beatFeedback != null);
                if (this.started && this.beat % song.BeatPerBar == 0)
                {
                    this.beatFeedbackState = BeatFeedbackState.Flash;
                }
                else
                {
                    this.beatFeedbackState = BeatFeedbackState.Beat;
                }

                Thread.Sleep(this.flashDurationInMsec);

                this.beatFeedbackState = BeatFeedbackState.Default;
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

        Console.WriteLine("End of background worker job.");
        this.resetEvent.Set();
    }

    private enum BeatFeedbackState
    {
        Default,
        Beat,
        Flash,
    }

    private enum SwipeMode
    {
        None,
        Vertical,
        Horizontal,
    }
}
