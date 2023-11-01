using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

using SongPrompter.Models;
using System.Text;

namespace SongPrompter.Services
{
    public interface IDataService
    {
        ObservableCollection<Playlist> Playlists { get; }

        void LoadPlaylist(string path);

        void RemovePlaylist(Playlist playlist);
    }

    internal partial class DataService : ObservableObject, IDataService
    {
        private const string PlaylistPreferencesName = "playlists";

        private ObservableCollection<Playlist> playlists = new ObservableCollection<Playlist>();

        private readonly Regex titleRegex = new Regex(@"^#\s(?<title>.*)");
        private readonly Regex artistRegex = new Regex(@"^##\s(?<artist>.*)");
        private readonly Regex metadataRegex = new Regex(@"^>\s(?<key>.*)\s*:\s*(?<value>.*)");
        private readonly Regex verseMetadataRegex = new Regex(@"\[(?<name>[\w\s]+)#(?<measure>\d+)\]");

        public DataService() 
        {
            string playlistPreferencesString = Preferences.Get(DataService.PlaylistPreferencesName, null);
            if (playlistPreferencesString != null)
            {
                string[] playlistPaths = playlistPreferencesString.Split(';');
                foreach (var path in playlistPaths)
                {
                    this.LoadPlaylist(path);
                }
            }
        }

        public ObservableCollection<Playlist> Playlists => this.playlists;

        public async void LoadPlaylist(string path)
        {
            Console.WriteLine($"Load playlist at path: {path}.");

            var folder = new DirectoryInfo(path);
            if (folder.Exists)
            {
                string[] files = null;
                PermissionStatus status = await Permissions.CheckStatusAsync<Permissions.StorageRead>();
                if (status == PermissionStatus.Denied)
                {
                    await Application.Current.MainPage.DisplayAlert("Alert", "Permission Denied", "OK");
                    // Ask for permission
                    status = await Permissions.RequestAsync<Permissions.StorageRead>();
                }

                if (status == PermissionStatus.Granted)
                {
                    files = Directory.GetFiles(path, "*");
                }

                if (files.Length > 0)
                {
                    System.Array.Sort(files);
                    Console.WriteLine($"Parse {files.Length} song files.");

                    List<Song> songs = new List<Song>();
                    foreach (string filePath in files)
                    {
                        try
                        {
                            Song song = this.ParseSongFile(filePath);
                            songs.Add(song);
                            Console.WriteLine($"Song {song.Name} loaded successfully.");
                        }
                        catch (Exception exception)
                        {
                            Console.WriteLine($"Exception thrown while loading song: {exception.Message}");
                        }
                    }

                    Playlist playlist = new Playlist()
                    {
                        Name = folder.Name,
                        Path = path,
                        Songs = songs.ToArray(),
                    };

                    this.playlists.Add(playlist);
                    this.OnPropertyChanged(nameof(this.Playlists));

                    this.SavePlaylistPreferences();
                }
                else
                {
                    Console.WriteLine($"No files found in folder.");
                }
            }
            else
            {
                Console.WriteLine($"Error: {path} does not exists.");
            }
        }

        public void RemovePlaylist(Playlist playlist)
        {
            if (this.playlists.Remove(playlist))
            {
                this.SavePlaylistPreferences();
            }
        }

        private void SavePlaylistPreferences()
        {
            if (this.Playlists.Count > 0)
            {
                string playlistPreferencesString = this.Playlists.Select(playlist => playlist.Path).Aggregate((left, right) => $"{left};{right}");
                Preferences.Set(DataService.PlaylistPreferencesName, playlistPreferencesString);
            }
            else
            {
                Preferences.Clear(DataService.PlaylistPreferencesName);
            }
        }

        private Song ParseSongFile(string filePath)
        {
            var song = new Song();

            using (FileStream reader = File.OpenRead(filePath))
            using (StreamReader textReader = new StreamReader(reader))
            {
                var verses = new List<Verse>();
                var parseLyrics = false;
                while (!textReader.EndOfStream)
                {
                    string line = textReader.ReadLine();

                    if (line.StartsWith("```"))
                    {
                        parseLyrics = !parseLyrics;
                        continue;
                    }

                    if (parseLyrics)
                    {
                        if (this.verseMetadataRegex.IsMatch(line))
                        {
                            Match match = this.verseMetadataRegex.Match(line);
                            verses.Add(new Verse()
                            {
                                Name = match.Groups["name"].Value,
                                StartBar = int.Parse(match.Groups["measure"].Value),
                                Lyrics = string.Empty,
                            });
                        }
                        else
                        {
                            if (verses.Count == 0)
                            {
                                verses.Add(new Verse()
                                {
                                    Name = string.Empty,
                                    StartBar = 0,
                                    Lyrics = string.Empty,
                                });
                            }

                            Verse verse = verses.Last();
                            if (!string.IsNullOrEmpty(verse.Lyrics))
                            {
                                verse.Lyrics += $"\n";
                            }

                            verse.Lyrics += line;
                        }
                    }
                    else
                    {
                        if (this.titleRegex.IsMatch(line))
                        {
                            song.Name = this.titleRegex.Match(line).Groups["title"].Value;
                        }
                        else if (this.artistRegex.IsMatch(line))
                        {
                            song.Artist = this.artistRegex.Match(line).Groups["artist"].Value;
                        }
                        else if (this.metadataRegex.IsMatch(line))
                        {
                            Match match = this.metadataRegex.Match(line);
                            switch (match.Groups["key"].Value)
                            {
                                case "bpm":
                                    song.Bpm = int.Parse(match.Groups["value"].Value);
                                    break;

                                case "metre":
                                    string[] values = match.Groups["value"].Value.Split('/');
                                    song.BeatPerBar = int.Parse(values[0]);
                                    song.BeatSubdivision = int.Parse(values[1]);
                                    break;

                                case "key":
                                    song.Key = match.Groups["value"].Value;
                                    break;

                                default:
                                    throw new Exception($"Unknown metadata: {match.Groups["key"].Value}");
                            }
                        }
                    }
                }

                song.Verses = verses.ToArray();
            }

            return song;
        }
    }
}
