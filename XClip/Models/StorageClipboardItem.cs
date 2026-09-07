using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.Input;
using FluentIcons.Common;

namespace XClip.Models;

public partial class StorageClipboardItem : AClipboardItem
{

    private Icon _icon;
    private string _content = string.Empty;

    public string Content
    {
        get => _content;
        set
        {
            if (value == _content) return;
            _content = value;
            OnPropertyChanged();
        }
    }
    public Icon Icon
    {
        get => _icon;
        set
        {
            _icon = value;
            OnPropertyChanged();
        }
    }

    public List<string> Paths
    {
        get;
        set
        {
            if (Equals(value, field)) return;
            field = value;
            SetIcon(value);
            UpdateTags(value);
            SetContent(value);
            OnPropertyChanged();
        }
    } = new List<string>();

    private void SetContent(List<string> value)
    {
        var contentBuilder = new System.Text.StringBuilder();
        var locations = string.Join(", ", value
            .Where(File.Exists)
            .Select(Path.GetDirectoryName)
            .Distinct());

        contentBuilder.Append($"Storage Items ({value.Count}) from {locations}:\n");

        foreach (var item in value)
        {
            if (File.Exists(item))
            {
                contentBuilder.AppendLine(Path.GetFileName(item));
            }
            else if (Directory.Exists(item))
            {
                // Path.GetFileName returns the directory's own name (e.g., "MyFolder")
                contentBuilder.AppendLine(Path.GetFileName(item.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)));
            }
        }

        Content = contentBuilder.ToString();
    }

    private void UpdateTags(List<string> value)
    {
        Tags.Clear();
        if (value.All(q => GetStorageType(q) == StorageType.Folder))
        {
            Tags.Add("Folder");
        }
        else if (value.All(q => GetStorageType(q) == StorageType.File))
        {
            Tags.Add("File");
        }
        else
        {
            Tags.Add("File");
            Tags.Add("Folder");
        }
    }

    private void SetIcon(List<string> value)
    {
        if (value.Count == 1)
        {
            var storageItem = value[0];
            if (GetStorageType(storageItem) == StorageType.File)
            {
                Icon = Icon.Document;
            }
            else if (GetStorageType(storageItem) == StorageType.Folder)
            {
                Icon = Icon.Folder;
            }
        }
        else
        {
            Icon = value.All(q => GetStorageType(q) == StorageType.Folder)
                ? Icon.FolderMultiple
                : value.All(q => GetStorageType(q) == StorageType.File)
                    ? Icon.DocumentMultiple
                    : Icon.Storage;
        }
    }

    private StorageType GetStorageType(string storageItem)
    {
        FileInfo fileInfo = new FileInfo(storageItem);
        if (fileInfo.Exists)
        {
            return StorageType.File;
        }

        if (Directory.Exists(storageItem))
        {
            return StorageType.Folder;
        }

        throw new FileNotFoundException($"The storage item '{storageItem}' does not exist.");
    }

    [RelayCommand]
    private void Delete()
    {
        OnDelete?.Invoke(this);
    }
}