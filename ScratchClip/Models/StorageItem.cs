using System.IO;
using ScratchClip.ViewModels;

namespace ScratchClip.Models;

public class StorageItem : ViewModelBase
{
    private string _filePath = string.Empty;
    private string? _iconPath;

    public string FilePath
    {
        get => _filePath;
        set 
        {
            _filePath = value;
            OnPropertyChanged();
        }
    }

    public string FullPath { get; init; } = string.Empty;

    public string? IconPath
    {
        get => _iconPath;
        set 
        {
            _iconPath = value;
            OnPropertyChanged();
        }
    }
    
    public bool IsFolder => Directory.Exists(FullPath);
    public bool IsFile => File.Exists(FullPath);
}