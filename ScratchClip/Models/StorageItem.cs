using System;
using System.IO;
using ScratchClip.ViewModels;

namespace ScratchClip.Models;

public class StorageItem : ViewModelBase
{
    private string _filePath = string.Empty;
    private string? _iconPath;

    public StorageItem()
    {
    }
    public string FilePath
    {
        get => _filePath;
        private set 
        {
            _filePath = value;
            OnPropertyChanged();
        }
    }

    public string FullPath
    {
        get;
        init
        {
            field = value;
            IsSvg = true;
            IsFolder = Directory.Exists(value);
            IsFile = File.Exists(value);
            FilePath = Path.GetFileName(
                value.TrimEnd(
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar));
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsFile));
            OnPropertyChanged(nameof(IsFolder));
        }
    } = string.Empty;

    public string? IconPath
    {
        get => _iconPath;
        set 
        {
            _iconPath = value;
            IsSvg = string.Equals(
                Path.GetExtension(value),
                ".svg",
                StringComparison.OrdinalIgnoreCase);
            OnPropertyChanged();
        }
    }

    public bool IsSvg
    {
        get;
        private set => SetProperty(ref field, value);
    }

    public bool IsFolder { get; init; }
    public bool IsFile { get; init; }
}