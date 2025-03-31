using System.Text.RegularExpressions;

using System.Collections.Generic;
using Data;
using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using Core.Services;

namespace Application;

internal partial class SetlistDatabase : ISingleton<App>
{
    private const string PlaylistPreferencesName = "playlists";

    private readonly Regex titleRegex = new(@"^#\s(?<title>.*)");
    private readonly Regex artistRegex = new(@"^##\s(?<artist>.*)");
    private readonly Regex metadataRegex = new(@"^>\s(?<key>.*)\s*:\s*(?<value>.*)");
    private readonly Regex verseMetadataRegex = new(@"\[(?<name>[\w\s]+)#(?<measure>\d+)\]");

    private AppSettings? appSettings;

    public SetlistDatabase() 
    {
        this.Data = [];
    }

    public List<Setlist> Data
    {
        get;
        private set;
    }

    public IEnumerator<Setlist> GetEnumerator()
    { 
        if (this.Data == null)
        {
            yield break;
        }

        foreach (Setlist element in this.Data)
        {
            yield return element;
        }
    }
    
    public void Bind(AppSettings appSettings)
    {
        this.appSettings = appSettings;
        foreach (var path in this.appSettings.SetlistPaths.Value)
        {
            this.LoadSetlist(path);
        }
    }

    public void LoadSetlist(string path)
    {
        Console.WriteLine($"Load playlist at path: {path}.");

        var folder = new DirectoryInfo(path);
        if (folder.Exists)
        {
            List<string> files = [.. Directory.GetFiles(path, "*")];

            if (files.Count > 0)
            {
                files.Sort();
                Console.WriteLine($"Parse {files.Count} song files.");

                List<Song> songs = [];
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

                Setlist playlist = new Setlist()
                {
                    Name = folder.Name,
                    Path = path,
                    Songs = songs.ToArray(),
                };

                this.Data.Add(playlist);

                this.SaveSetlistPreferences();
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

    public void RemoveSetlist(Setlist playlist)
    {
        if (this.Data.Remove(playlist))
        {
            this.SaveSetlistPreferences();
        }
    }

    private void SaveSetlistPreferences()
    {
        Debug.Assert(this.appSettings != null);
        this.appSettings.SetlistPaths.Value = [.. this.Data.Select(setlist => setlist.Path)];
        this.appSettings.Save(App.Version);
    }

    private Song ParseSongFile(string filePath)
    {
        var song = new Song();

        using (FileStream reader = File.OpenRead(filePath))
        using (StreamReader textReader = new(reader))
        {
            var verses = new List<Verse>();
            var parseLyrics = false;
            while (!textReader.EndOfStream)
            {
                string? line = textReader.ReadLine();
                Debug.Assert(line != null);
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
