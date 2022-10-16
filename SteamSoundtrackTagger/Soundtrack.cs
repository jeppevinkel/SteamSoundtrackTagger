using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace SteamSoundtrackTagger;

public class Soundtrack : INotifyPropertyChanged
{
    private string _path;
    private string _name;
    private string? _appid;
    private readonly ObservableCollection<TrackFile> _trackFiles = new();

    private Regex _appidRegEx = new Regex("\\((\\d+)\\)");

    public string Path
    {
        get => _path;
        set
        {
            _path = value;
            OnPropertyChanged();
        }
    }

    public string Name
    {
        get => _name;
        set
        {
            _name = value;
            OnPropertyChanged();
        }
    }

    public string? AppId
    {
        get => _appid;
        set
        {
            _appid = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<TrackFile> TrackFiles => _trackFiles;

    public int TrackCount => TrackFiles.Count;

    public Soundtrack(string path, string? appId = null)
    {
        if (!Directory.Exists(path))
        {
            return;
        }
        var dirInfo = new DirectoryInfo(path);
        Path = path;
        Name = dirInfo.Name;
        if (appId is not null)
        {
            AppId = appId;
        }

        var match = _appidRegEx.Match(Name);
        Name = _appidRegEx.Replace(Name, "").Trim();

        if (match.Groups.Count >= 2)
        {
            AppId = match.Groups[1].Value;
        }
        
        var files = dirInfo.GetFiles().Where(f => f.Extension == ".mp3");

        if (files.Count() > 0)
        {
            foreach (var fileInfo in files)
            {
                _trackFiles.Add(new TrackFile(fileInfo.FullName));
            }
        }
        else
        {
            foreach (var subDirectory in dirInfo.EnumerateDirectories())
            {
                files = subDirectory.GetFiles().Where(f => f.Extension == ".mp3");

                if (files.Count() > 0)
                {
                    foreach (var fileInfo in files)
                    {
                        _trackFiles.Add(new TrackFile(fileInfo.FullName));
                    }
                }
            }
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
}