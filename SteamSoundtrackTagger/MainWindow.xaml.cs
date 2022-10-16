using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using Gameloop.Vdf;
using Gameloop.Vdf.Linq;
using HtmlAgilityPack;
using Microsoft.Win32;
using Ookii.Dialogs.Wpf;
using SteamSoundtrackTagger.Api;

namespace SteamSoundtrackTagger
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private string _selectedPath = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
        private string _steamCmdPath;
        private readonly ObservableCollection<Soundtrack> _soundtracks = new();
        private readonly ObservableCollection<Soundtrack> _completedSoundtracks = new();
        private readonly ObservableCollection<Track> _tracks = new();
        private static readonly HttpClient HttpClient = new HttpClient();
        private readonly List<string> _libraryPaths = new List<string>();
        private string curentProgressLabelMain = "";
        private double currentProgressMain = 0;
        private string curentProgressLabelSec = "";
        private double currentProgressSec = 0;

        public static string LibraryManifestPath = @".\steamapps\libraryfolders.vdf";
        public static readonly Regex ExtractSteamPath = new Regex("\"(.+)\\\\");
        public static readonly string SteamShellRegPath = @"HKEY_CLASSES_ROOT\steam\Shell\Open\Command";

        public string SelectedPath
        {
            get => _selectedPath;
            set
            {
                _selectedPath = value;
                OnPropertyChanged();
            }
        }

        public string SteamCmdPath
        {
            get => _steamCmdPath;
            set
            {
                _steamCmdPath = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<Soundtrack> Soundtracks => _soundtracks;
        public ObservableCollection<Soundtrack> CompletedSoundtracks => _completedSoundtracks;

        public ObservableCollection<Track> Tracks => _tracks;

        public MainWindow()
        {
            DataContext = this;
            InitializeComponent();

            Debug.WriteLine(GetSteamPath());
            LibraryManifestPath = Path.Combine(GetSteamPath(), LibraryManifestPath);

            BtnScanFolders.Click += BtnScanFoldersOnClick;

            BtnScrape.Click += (sender, args) =>
            {
                BackgroundWorker worker = new BackgroundWorker();
                worker.WorkerReportsProgress = true;
                worker.DoWork += ScrapeWorker_DoWork;
                worker.ProgressChanged += ScrapeWorker_ProgressChanged;

                completedSoundtrack = null;

                worker.RunWorkerAsync();
            };
        }
        
        Soundtrack? completedSoundtrack;
        private void ScrapeWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            float index = 0f;
            foreach (var soundtrack in Soundtracks)
            {
                if (soundtrack.AppId == null)
                    continue;
                var appId = soundtrack.AppId;
                curentProgressLabelMain = soundtrack.Name;
                (sender as BackgroundWorker).ReportProgress(0);
                Debug.WriteLine(appId);
                if (appId == null)
                    return;

                var appInfo = GetAppInfo(appId).Result;
                if (appInfo is null) continue;

                var hash = appInfo?.AlbumMetadata.CdnAssets.AlbumCover;

                float index2 = 0f;
                var tracks = new List<Track>();
                foreach (var track in appInfo.AlbumMetadata.Tracks.Values)
                {
                    var name = track.OriginalName;
                    curentProgressLabelSec = track.OriginalName;
                    (sender as BackgroundWorker).ReportProgress(0);

                    var closestDistance = int.MaxValue;
                    var closestFile = "";
                    //TODO: Fix crash if no files are found.
                    foreach (var trackFile in soundtrack.TrackFiles)
                    {
                        var distance = name.Levenshtein(trackFile.Name);
                        if (distance >= closestDistance) continue;
                        closestDistance = distance;
                        closestFile = trackFile.Path;
                    }

                    var finalTrack = new Track(track.TrackNumber, track.OriginalName,
                        $"{track.Minutes}:{track.Seconds}", closestFile);

                    var tFile = TagLib.File.Create(finalTrack.Path);
                    if (!tFile.Tag.Genres.Contains("Soundtrack"))
                        tFile.Tag.Genres = tFile.Tag.Genres.Append("Soundtrack").ToArray();
                    tFile.Tag.Title = finalTrack.Name;
                    if (appInfo.AlbumMetadata.Metadata.Artist.English is not null)
                    {
                        tFile.Tag.AlbumArtists = new[] {appInfo.AlbumMetadata.Metadata.Artist.English};
                    }

                    if (appInfo.AlbumMetadata.Metadata.Composer.English is not null)
                    {
                        tFile.Tag.Composers = new[] {appInfo.AlbumMetadata.Metadata.Composer.English};
                    }

                    tFile.Tag.Track = finalTrack.Id;

                    tFile.Tag.Album = appInfo.Common.Name;
                    tFile.Save();

                    tracks.Add(finalTrack);

                    if (hash is not null)
                    {
                        SteamCmdPath = GetAlbumCoverUrl(appId, hash);
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri(GetAlbumCoverUrl(appId, hash));
                        bitmap.EndInit();

                        bitmap.DownloadCompleted += (o, eventArgs) =>
                        {
                            bitmap.SavePngImage(Path.Combine(soundtrack.Path, "cover.png"));
                        };

                        // ImageAlbumCover.Source = bitmap;
                    }

                    currentProgressSec = (++index2 / appInfo.AlbumMetadata.Tracks.Values.Count) * 100;
                    (sender as BackgroundWorker).ReportProgress(0);
                }
                
                completedSoundtrack = soundtrack;
                currentProgressMain = (++index / Soundtracks.Count) * 100;
                (sender as BackgroundWorker).ReportProgress(1);
            }

            (sender as BackgroundWorker).RunWorkerCompleted += (o, args) =>
            {
                // CompletedSoundtracks.Clear();
                // foreach (var soundtrack in completedSoundtracks)
                // {
                //     CompletedSoundtracks.Add(soundtrack);
                // }
            };
        }

        private void ScrapeWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            ProgressBarMain.Value = currentProgressMain;
            ProgressLabelMain.Text = curentProgressLabelMain;
            ProgressBarSec.Value = currentProgressSec;
            ProgressLabelSec.Text = curentProgressLabelSec;

            if (e.ProgressPercentage == 1) CompletedSoundtracks.Add(completedSoundtrack);
        }

        private void BtnScanFoldersOnClick(object sender, RoutedEventArgs e)
        {
            BackgroundWorker worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += FolderScanWorker_DoWork;
            worker.ProgressChanged += FolderScanWorker_ProgressChanged;

            worker.RunWorkerAsync();
        }

        private void FolderScanWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            var libraries = VdfConvert.Deserialize(File.ReadAllText(LibraryManifestPath));
            foreach (var library in libraries.Value!.Children<VProperty>())
            {
                _libraryPaths.Add(library.Value["path"].ToString());
            }

            // Soundtracks.Clear();
            var soundtracks = new List<Soundtrack>();
            foreach (var libraryPath in _libraryPaths)
            {
                var steamapps = Path.Combine(libraryPath, "steamapps");
                var manifests = Directory.GetFiles(steamapps, "*.acf");

                float index = 0f;
                foreach (var manifest in manifests)
                {
                    Debug.Write(manifest + " : ");
                    var info = VdfConvert.Deserialize(File.ReadAllText(manifest));
                    bool isSoundtrack = info.Value["UserConfig"]["highqualityaudio"] is not null;

                    if (isSoundtrack)
                    {
                        string installDir = Path.Combine(steamapps, "music", info.Value["installdir"].ToString());
                        soundtracks.Add(new Soundtrack(installDir, info.Value["appid"].ToString()));
                    }

                    (sender as BackgroundWorker).ReportProgress((int) (((++index) / manifests.Length) * 100));
                }
            }

            (sender as BackgroundWorker).RunWorkerCompleted += (o, args) =>
            {
                Soundtracks.Clear();
                foreach (var soundtrack in soundtracks)
                {
                    Soundtracks.Add(soundtrack);
                }
            };
        }

        private void FolderScanWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            ProgressBarMain.Value = e.ProgressPercentage;
        }

        private void OpenDirectorySelect()
        {
            var directDia = new VistaFolderBrowserDialog();
            directDia.Description = "Please select a folder with Steam soundtracks.";
            directDia.UseDescriptionForTitle = true;
            directDia.SelectedPath = SelectedPath;

            if (directDia.ShowDialog() ?? false)
            {
                SelectedPath = directDia.SelectedPath;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        private static async Task<string> CallUrl(string fullUrl)
        {
            var cookieContainer = new CookieContainer();
            using var handler = new HttpClientHandler() {CookieContainer = cookieContainer};
            using var client = new HttpClient(handler);
            cookieContainer.Add(new Uri(fullUrl), new Cookie("birthtime", "0"));
            var response = await client.GetStringAsync(fullUrl).ConfigureAwait(false);
            return response;
        }

        private static async Task<AppInfo?> GetAppInfo(string appid)
        {
            HttpClient.DefaultRequestHeaders.Clear();
            HttpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            HttpClient.DefaultRequestHeaders.Add("User-Agent", "Steam Soundtrack Tagger");
            // HttpClient.Timeout = TimeSpan.FromSeconds(5);

            try
            {
                var response = await HttpClient.GetStreamAsync($"https://api.steamcmd.net/v1/info/{appid}")
                    .ConfigureAwait(false);
                var jsonNode = await JsonSerializer.DeserializeAsync<JsonNode>(response);
                return jsonNode?["data"]?[appid].Deserialize<AppInfo>();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }

            return null;
        }

        private static IEnumerable<Track> ExtractTracks(string html)
        {
            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(html);

            var trackContainers = htmlDoc.DocumentNode.Descendants("div")
                .Where(node => node.GetAttributeValue("class", "").Contains("music_album_track_ctn"))
                .ToList();

            List<Track> tracks = new();

            foreach (var trackContainer in trackContainers)
            {
                var id = trackContainer.SelectSingleNode("./div[contains(@class, 'music_album_track_number')]")
                    .InnerText.Trim();
                var name = trackContainer
                    .SelectSingleNode(
                        "./div[contains(@class, 'music_album_track_name_ctn')]/div[contains(@class, 'music_album_track_name')]")
                    .InnerText.Trim();
                var duration = trackContainer
                    .SelectSingleNode("./div[contains(@class, 'music_album_track_duration')]").InnerText.Trim();
                tracks.Add(new Track(id, name, duration));
            }

            return tracks;
        }

        public static IEnumerable<Track> GetTracks(string appId)
        {
            var url = $"https://store.steampowered.com/app/{appId}/";
            var response = CallUrl(url).Result;

            return ExtractTracks(response);
        }

        private static string GetAlbumCoverUrl(string appid, string hash)
        {
            return $"https://cdn.cloudflare.steamstatic.com/steamcommunity/public/images/apps/{appid}/{hash}.jpg";
        }

        public static string GetSteamPath()
        {
            //TODO: Add warning message instead of crashing if Steam isn't installed.
            var installPath = (string) Registry.GetValue(@"HKEY_CLASSES_ROOT\steam\Shell\Open\Command", "", null)!;
            return ExtractSteamPath.Match(installPath).Groups[1].Value;
        }
    }
}