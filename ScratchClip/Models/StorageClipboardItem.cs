using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using FluentIcons.Common;
using ScratchClip.Helper;

namespace ScratchClip.Models;

public partial class StorageClipboardItem : AClipboardItem
{
    private readonly Timer _lazyUpdateTimer = new(800);

    [ObservableProperty] private List<StorageItem> _storageItems;

    public StorageClipboardItem()
    {
        ClipboardType = ClipboardType.Storage;
        _lazyUpdateTimer.Elapsed += LazyUpdateTimerOnElapsed;
    }

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
            UpdateStorageItems(value);
            OnPropertyChanged();
        }
    } = new();

    public List<string> Files => Paths.Where(File.Exists).ToList();
    public List<string> Folders => Paths.Where(Directory.Exists).ToList();

    public override string SuggestedFile
    {
        get
        {
            if (Paths.Count == 1 &&
                GetStorageType(Paths[0]) == StorageType.File)
                return Path.GetFileName(Paths[0]);

            var locations = string.Join(", ", Paths
                .Where(File.Exists)
                .Select(Path.GetDirectoryName)
                .Distinct());
            return $"{locations}.zip";
        }
    }

    private void LazyUpdateTimerOnElapsed(object? sender, ElapsedEventArgs e)
    {
        _lazyUpdateTimer.Stop();
        var list = StorageItems
            .Where(q => !string.IsNullOrEmpty(q.FullPath))
            .ToList();
        foreach (var storageItem in list) storageItem.IconPath = LinuxFileIconService.GetIconPath(storageItem.FullPath);

        Dispatcher.UIThread.Post(async void () =>
        {
            foreach (var storageItem in list)
                StorageItems[StorageItems.IndexOf(storageItem)].IconPath = storageItem.IconPath;
        }, DispatcherPriority.Background);
    }

    private void UpdateStorageItems(List<string> value)
    {
        StorageItems = Paths
            .Where(path =>
                File.Exists(path) ||
                Directory.Exists(path))
            .Select(path => new StorageItem
            {
                FullPath = path
            })
            .ToList();
        _lazyUpdateTimer.Start();
    }


    private void SetContent(List<string> value)
    {
        var contentBuilder = new StringBuilder();
        var locations = string.Join(", ", value
            .Where(File.Exists)
            .Select(Path.GetDirectoryName)
            .Distinct());

        contentBuilder.Append($"Storage Items ({value.Count}) from {locations}:\n");

        foreach (var item in value)
            if (File.Exists(item))
                contentBuilder.AppendLine(Path.GetFileName(item));
            else if (Directory.Exists(item))
                // Path.GetFileName returns the directory's own name (e.g., "MyFolder")
                contentBuilder.AppendLine(Path.GetFileName(item.TrimEnd(Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar)));

        Text = contentBuilder.ToString();
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
                Icon = Icon.Document;
            else if (GetStorageType(storageItem) == StorageType.Folder) Icon = Icon.Folder;
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
        var fileInfo = new FileInfo(storageItem);
        if (fileInfo.Exists) return StorageType.File;

        if (Directory.Exists(storageItem)) return StorageType.Folder;

        return StorageType.Invalid;
    }

    public override async Task<object?> GetData()
    {
        // Single file: return an async FileStream
        if (Paths.Count == 1 &&
            GetStorageType(Paths[0]) == StorageType.File)
            return new FileStream(
                Paths[0],
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                1024 * 64,
                FileOptions.Asynchronous | FileOptions.SequentialScan);

        // Multiple files/folders -> temporary ZIP on disk
        var tempZip = Path.Combine(
            Path.GetTempPath(),
            $"{Guid.NewGuid():N}.zip");

        await using (var zipStream = new FileStream(
                         tempZip,
                         FileMode.Create,
                         FileAccess.Write,
                         FileShare.None,
                         1024 * 64,
                         FileOptions.Asynchronous))
        {
            using var archive = new ZipArchive(
                zipStream,
                ZipArchiveMode.Create,
                false);

            foreach (var path in Paths)
                if (File.Exists(path))
                    await AddFileToZip(
                        archive,
                        path,
                        Path.GetFileName(path));
                else if (Directory.Exists(path))
                    await AddDirectoryToZip(
                        archive,
                        path,
                        Path.GetFileName(path));
        }

        // Return the completed ZIP as an async FileStream
        return new FileStream(
            tempZip,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            1024 * 64,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
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
            1024 * 64,
            FileOptions.Asynchronous | FileOptions.SequentialScan);

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
            await AddDirectoryToZip(
                archive,
                subDirectory,
                Path.Combine(
                    entryRoot,
                    Path.GetFileName(subDirectory)));
    }

    public override void Delete()
    {
        OnDelete?.Invoke(this);
    }

    public override Task OpenItem()
    {
        //get temporary file path
        var tempFilePath = "";
        if (Paths.Count > 0 && File.Exists(Paths[0]))
            tempFilePath = Path.GetDirectoryName(Paths[0]);
        else
            tempFilePath = Path.GetDirectoryName(Folders[0]);
        if (string.IsNullOrEmpty(tempFilePath)) return Task.CompletedTask;
        Process.Start(new ProcessStartInfo(tempFilePath)
            { UseShellExecute = true });
        return Task.CompletedTask;
    }
}