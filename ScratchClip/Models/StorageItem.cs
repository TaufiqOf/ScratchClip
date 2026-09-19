using System;
using System.IO;
using ScratchClip.ViewModels;

namespace ScratchClip.Models;

public class StorageItem : ViewModelBase, IDisposable
{
    private FileSystemWatcher? _watcher;
    private string? _iconPath;

    public string FullPath
    {
        get;
        init
        {
            field = value;

            FilePath = Path.GetFileName(
                value.TrimEnd(
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar));
            IsSvg = true;
            Type = GetStorageType(value);

            InitializeWatcher();

            OnPropertyChanged();
        }
    } = string.Empty;

    public string FilePath { get; private set; } = string.Empty;

    public StorageType Type
    {
        get;
        private set;
    } = StorageType.Invalid;

    public bool IsValid =>
        Type != StorageType.Invalid;

    public string? IconPath
    {
        get => _iconPath;
        set
        {
            if (SetProperty(ref _iconPath, value))
            {
                IsSvg = string.Equals(
                    Path.GetExtension(value),
                    ".svg",
                    StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    public bool IsSvg 
    {
        get;
        private set => SetProperty(ref field, value);
    }

    public bool Exists =>
        Type switch
        {
            StorageType.File => File.Exists(FullPath),
            StorageType.Folder => Directory.Exists(FullPath),
            _ => false
        };

    private static StorageType GetStorageType(string path)
    {
        if (Directory.Exists(path))
            return StorageType.Folder;

        if (File.Exists(path))
            return StorageType.File;

        return StorageType.Invalid;
    }

    private void InitializeWatcher()
    {
        var parentDirectory = Path.GetDirectoryName(
            FullPath.TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar));

        if (string.IsNullOrEmpty(parentDirectory) ||
            !Directory.Exists(parentDirectory))
        {
            return;
        }

        _watcher = new FileSystemWatcher(parentDirectory)
        {
            NotifyFilter =
                NotifyFilters.FileName |
                NotifyFilters.DirectoryName |
                NotifyFilters.LastWrite |
                NotifyFilters.Size,

            IncludeSubdirectories = false,
            EnableRaisingEvents = true
        };

        _watcher.Changed += OnStorageChanged;
        _watcher.Created += OnStorageChanged;
        _watcher.Deleted += OnStorageChanged;
        _watcher.Renamed += OnStorageRenamed;
        _watcher.Error += OnWatcherError;
    }

    private void OnStorageChanged(
        object sender,
        FileSystemEventArgs e)
    {
        if (!IsRelatedPath(e.FullPath))
            return;

        StorageChanged();
    }

    private void OnStorageRenamed(
        object sender,
        RenamedEventArgs e)
    {
        if (!IsRelatedPath(e.FullPath) &&
            !IsRelatedPath(e.OldFullPath))
        {
            return;
        }

        StorageChanged();
    }

    private bool IsRelatedPath(string path)
    {
        return string.Equals(
            NormalizePath(path),
            NormalizePath(FullPath),
            StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizePath(string path)
    {
        return Path.TrimEndingDirectorySeparator(
            Path.GetFullPath(path));
    }

    private void StorageChanged()
    {
        var oldType = Type;

        Type = GetStorageType(FullPath);

        if (oldType != Type)
        {
            OnPropertyChanged(nameof(Type));
            OnPropertyChanged(nameof(IsValid));
        }

        OnPropertyChanged(nameof(Exists));
    }

    private void OnWatcherError(
        object sender,
        ErrorEventArgs e)
    {
        // TODO: log watcher errors
    }

    public void Dispose()
    {
        if (_watcher is null)
            return;

        _watcher.EnableRaisingEvents = false;

        _watcher.Changed -= OnStorageChanged;
        _watcher.Created -= OnStorageChanged;
        _watcher.Deleted -= OnStorageChanged;
        _watcher.Renamed -= OnStorageRenamed;
        _watcher.Error -= OnWatcherError;

        _watcher.Dispose();
        _watcher = null;
    }
}