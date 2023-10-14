using CommunityToolkit.Mvvm.ComponentModel;
using System.Diagnostics;

using SongPrompter.Models;

namespace SongPrompter.ViewModels
{
    public partial class SongViewModel : ObservableObject
    {
        [ObservableProperty]
        private Playlist playlist;

        [ObservableProperty]
        private Song currentSong;

        [ObservableProperty]
        private string lyrics;

        public int nextSongIndex = 0;

        public SongViewModel()
        {
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
                this.CurrentSong = this.Playlist.Songs[this.nextSongIndex];
                this.Lyrics = this.CurrentSong.Lyrics.Aggregate((left, right) => left + '\n' + right);
                this.nextSongIndex++;
            }
        }
    }
}

