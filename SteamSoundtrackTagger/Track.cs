using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SteamSoundtrackTagger;

public class Track : INotifyPropertyChanged
{
    private uint _id;
    private string _name;
    private string? _durration;
    private string? _path;

    public uint Id
    {
        get => _id;
        set
        {
            _id = value;
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

    public string? Duration
    {
        get => _durration;
        set
        {
            _durration = value;
            OnPropertyChanged();
        }
    }

    public string? Path
    {
        get => _path;
        set
        {
            _path = value;
            OnPropertyChanged();
        }
    }

    public Track(string id, string name, string duration, string? path = null)
    {
        if (uint.TryParse(id, out _id))
        {
            OnPropertyChanged(nameof(Id));
            Name = name;
            Duration = duration;
            Path = path;
        }
        else
        {
            throw new ArgumentException("Argument must be of an integer value.", nameof(id));
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