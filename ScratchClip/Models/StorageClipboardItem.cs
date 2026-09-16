using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using FluentIcons.Common;
using ScratchClip.Helper;

namespace ScratchClip.Models;

public partial class StorageClipboardItem : AClipboardItem
{
    Timer _lazyUpdateTimer = new Timer(500);

    [ObservableProperty]
    private List<StorageItem> _storageItems;

    public string Content
    {
        get;
        set
        {
            if (value == field) return;
            field = value;
            OnPropertyChanged();
        }
    } = string.Empty;

    public Icon Icon
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    }

    public StorageClipboardItem()
    {
        _lazyUpdateTimer.Elapsed += LazyUpdateTimerOnElapsed;
        _lazyUpdateTimer.Start();
    }

    void LazyUpdateTimerOnElapsed(object? sender, ElapsedEventArgs e)
    {
        _lazyUpdateTimer.Stop();

        Dispatcher.UIThread.Post(async void () =>
        {
            StorageItems = await GetStorageItems();
        });
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

    public List<string> Files => Paths.Where(File.Exists).ToList();
    public List<string> Folders => Paths.Where(Directory.Exists).ToList();



    private Task<List<StorageItem>> GetStorageItems()
    {
        return Task.Factory.StartNew(() =>
        {
            return Paths
                .Where(path =>
                    File.Exists(path) ||
                    Directory.Exists(path))
                .Select(path => new StorageItem
                {
                    FilePath = Path.GetFileName(
                        path.TrimEnd(
                            Path.DirectorySeparatorChar,
                            Path.AltDirectorySeparatorChar)),
                    FullPath = path,
                    IconPath =
                        LinuxFileIconService.GetIconPath(path)
                })
                .ToList();
        });
    }

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
                contentBuilder.AppendLine(Path.GetFileName(item.TrimEnd(Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar)));
            }
        }

        Content = contentBuilder.ToString();
    }

    private void UpdateTags(List<string> value)
    {
        Tags.Clear();
        if (value.All(q => GetStorageType(q) == StorageType.Folder))
        {
            Tags.Add("FOLDER");
        }
        else if (value.All(q => GetStorageType(q) == StorageType.File))
        {
            Tags.Add("FILE");
        }
        else
        {
            Tags.Add("FILE");
            Tags.Add("FOLDER");
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

    public override async Task<object?> GetData()
    {
        // Single file: return an async FileStream
        if (Paths.Count == 1 &&
            GetStorageType(Paths[0]) == StorageType.File)
        {
            return new FileStream(
                Paths[0],
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 1024 * 64,
                options: FileOptions.Asynchronous | FileOptions.SequentialScan);
        }

        // Multiple files/folders -> temporary ZIP on disk
        var tempZip = Path.Combine(
            Path.GetTempPath(),
            $"{Guid.NewGuid():N}.zip");

        await using (var zipStream = new FileStream(
                         tempZip,
                         FileMode.Create,
                         FileAccess.Write,
                         FileShare.None,
                         bufferSize: 1024 * 64,
                         options: FileOptions.Asynchronous))
        {
            using var archive = new ZipArchive(
                zipStream,
                ZipArchiveMode.Create,
                leaveOpen: false);

            foreach (var path in Paths)
            {
                if (File.Exists(path))
                {
                    await AddFileToZip(
                        archive,
                        path,
                        Path.GetFileName(path));
                }
                else if (Directory.Exists(path))
                {
                    await AddDirectoryToZip(
                        archive,
                        path,
                        Path.GetFileName(path));
                }
            }
        }

        // Return the completed ZIP as an async FileStream
        return new FileStream(
            tempZip,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 1024 * 64,
            options: FileOptions.Asynchronous | FileOptions.SequentialScan);
    }

    private static async Task AddFileToZip(
        ZipArchive archive,
        string filePath,
        string entryName)
    {
        var entry = archive.CreateEntry(
            entryName,
            CompressionLevel.Fastest);

        await using var source = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 1024 * 64,
            options: FileOptions.Asynchronous | FileOptions.SequentialScan);

        await using var target = entry.Open();

        await source.CopyToAsync(target);
    }

    private static async Task AddDirectoryToZip(
        ZipArchive archive,
        string directory,
        string entryRoot)
    {
        foreach (var file in Directory.GetFiles(directory))
        {
            var relativePath = Path.GetRelativePath(
                directory,
                file);

            await AddFileToZip(
                archive,
                file,
                Path.Combine(entryRoot, relativePath));
        }

        foreach (var subDirectory in Directory.GetDirectories(directory))
        {
            await AddDirectoryToZip(
                archive,
                subDirectory,
                Path.Combine(
                    entryRoot,
                    Path.GetFileName(subDirectory)));
        }
    }

    public override string SuggestedFile
    {
        get
        {
            if (Paths.Count == 1 &&
                GetStorageType(Paths[0]) == StorageType.File)
            {
                return Path.GetFileName(Paths[0]);
            }
            else
            {
                var locations = string.Join(", ", Paths
                    .Where(File.Exists)
                    .Select(Path.GetDirectoryName)
                    .Distinct());
                return $"{locations}.zip";
            }
        }
    }

    public override void Delete()
    {
        OnDelete?.Invoke(this);
    }
}