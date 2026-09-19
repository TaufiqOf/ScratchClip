using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;
using ScratchClip.Models;

namespace ScratchClip.Services.ClipboardService;

internal class StorageClipboardService : AClipboardService
{
    private Process? _xclipProcess;

    public override async Task<AClipboardItem?> GetItemAsync(
        ClipboardDataFormat type)
    {
        Console.WriteLine(
            $"StorageClipboardService.GetItemAsync({type})");
        var clipboard = GetClipboard();

        if (clipboard == null)
            return null;

        var items = await GetStorageItemsAsync(clipboard);

        if (items == null || items.Count == 0)
            return null;

        var paths = items
            .Select(item =>
                item.Path.IsAbsoluteUri
                    ? item.Path.LocalPath
                    : item.Path.ToString())
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .ToList();

        if (paths.Count == 0)
            return null;

        var displayText = paths.Count == 1
            ? Path.GetFileName(paths[0])
            : $"{paths.Count} items";

        // Don't call TryGetDataAsync() here.
        // It can cause delays with X11 clipboard ownership.
        return new StorageClipboardItem
        {
            Format = ClipboardDataFormat.Storage,
            Timestamp = DateTime.Now,
            DisplayText = displayText,
            Paths = paths
        };
    }

    public override Task CreateSignature(AClipboardItem item)
    {
        Console.WriteLine(
            $"StorageClipboardService.CreateSignature({item})");
        if (item is not StorageClipboardItem storageItem ||
            storageItem.Paths.Count == 0)
            return Task.CompletedTask;

        return Task.Run(() =>
        {
            var sb = new StringBuilder();

            foreach (var path in storageItem.Paths
                         .OrderBy(p => p, StringComparer.Ordinal))
            {
                sb.Append(path);

                if (File.Exists(path))
                    sb.Append(':')
                        .Append(File.GetLastWriteTimeUtc(path).Ticks);
                else if (Directory.Exists(path))
                    sb.Append(':')
                        .Append(Directory.GetLastWriteTimeUtc(path).Ticks);

                sb.Append(';');
            }

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());

            item.Signature =
                Convert.ToHexString(
                    SHA256.HashData(bytes));
        });
    }

    public override async Task CopyData(AClipboardItem value)
    {
        Console.WriteLine(
            $"StorageClipboardService.CopyData({value})");

        if (value is not StorageClipboardItem storageItem ||
            storageItem.Paths.Count == 0)
        {
            Console.WriteLine("No storage item.");
            return;
        }

        if (!OperatingSystem.IsLinux())
        {
            await CopyDataWithAvaloniaAsync(storageItem);
            return;
        }

        await CopyDataWithXclipAsync(storageItem.Paths);
    }

    private static string GetXclipPath()
    {
        // 1. xclip next to the application executable.
        // This is the AppImage case, and also works for a normal
        // extracted/deployed application directory.
        var localXclip = Path.Combine(
            AppContext.BaseDirectory,
            "xclip");

        if (File.Exists(localXclip))
            return localXclip;

        // 2. Normal Linux installation/development.
        // Let the OS find xclip through PATH.
        return "xclip";
    }

    private async Task CopyDataWithXclipAsync(
        IReadOnlyList<string> paths)
    {
        var xclip = GetXclipPath();

        var uris = paths
            .Where(p => File.Exists(p) || Directory.Exists(p))
            .Select(p => new Uri(Path.GetFullPath(p)).AbsoluteUri)
            .ToList();

        if (uris.Count == 0)
        {
            Console.WriteLine("No valid files.");
            return;
        }

        var payload =
            "copy\n" +
            string.Join("\n", uris);

        StopXclip();

        var psi = new ProcessStartInfo
        {
            FileName = xclip,
            UseShellExecute = false,
            RedirectStandardInput = true,
            CreateNoWindow = true
        };

        psi.ArgumentList.Add("-selection");
        psi.ArgumentList.Add("clipboard");

        psi.ArgumentList.Add("-target");
        psi.ArgumentList.Add("x-special/gnome-copied-files");

        psi.ArgumentList.Add("-loops");
        psi.ArgumentList.Add("0");

        // IMPORTANT: keep xclip in the foreground so our Process
        // represents the actual clipboard owner.
        psi.ArgumentList.Add("-quiet");

        var process = new Process
        {
            StartInfo = psi,
            EnableRaisingEvents = true
        };

        process.Exited += (sender, _) =>
        {
            if (sender is not Process exitedProcess)
                return;
            Console.WriteLine($"xclip exited: {exitedProcess.ExitCode}");

            if (ReferenceEquals(_xclipProcess, exitedProcess))
                _xclipProcess = null;

            exitedProcess.Dispose();
        };

        if (!process.Start())
        {
            process.Dispose();
            throw new InvalidOperationException(
                $"Could not start xclip: {xclip}");
        }

        _xclipProcess = process;

        await process.StandardInput.WriteAsync(payload);
        await process.StandardInput.FlushAsync();
        process.StandardInput.Close();

        Console.WriteLine(
            $"X11 clipboard set with {uris.Count} file(s)");
    }

    private async Task CopyDataWithAvaloniaAsync(
        StorageClipboardItem storageItem)
    {
        var clipboard = GetClipboard();
        var storageProvider = GetStorageProvider();

        if (clipboard == null ||
            storageProvider == null)
            return;

        var files = new List<IStorageItem>();

        foreach (var path in storageItem.Paths)
        {
            IStorageItem? item = null;

            if (File.Exists(path))
                item =
                    await storageProvider
                        .TryGetFileFromPathAsync(path);
            else if (Directory.Exists(path))
                item =
                    await storageProvider
                        .TryGetFolderFromPathAsync(path);

            if (item != null)
                files.Add(item);
        }

        if (files.Count == 0)
            return;

        var transfer = new DataTransfer();

        foreach (var file in files)
            transfer.Add(
                DataTransferItem.Create(
                    DataFormat.File,
                    file));

        await clipboard.SetDataAsync(transfer);
    }

    private void StopXclip()
    {
        var process = _xclipProcess;

        if (process == null)
            return;

        _xclipProcess = null;

        try
        {
            if (!process.HasExited)
            {
                process.Kill();

                if (!process.WaitForExit(1000))
                    Console.WriteLine(
                        "xclip did not exit within 1 second.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"Error stopping xclip: {ex.Message}");
        }
        finally
        {
            process.Dispose();
        }
    }

    public override async Task<object?> GetClipboardData()
    {
        Console.WriteLine(
            "StorageClipboardService.GetClipboardData()");
        var clipboard = GetClipboard();

        if (clipboard == null)
            return null;

        return await GetStorageItemsAsync(clipboard);
    }

    public override Task<bool> IsDataSame(
        AClipboardItem existingItem,
        object data)
    {
        Console.WriteLine(
            $"StorageClipboardService.IsDataSame({existingItem}, {data})");
        if (existingItem is not StorageClipboardItem storageItem)
            return Task.FromResult(false);

        var currentPaths = ExtractPaths(data);

        if (currentPaths == null)
            return Task.FromResult(false);

        if (storageItem.Paths.Count != currentPaths.Count)
            return Task.FromResult(false);

        return Task.FromResult(
            storageItem.Paths.SequenceEqual(
                currentPaths,
                StringComparer.Ordinal));
    }

    private static async Task<IReadOnlyList<IStorageItem>?>
        GetStorageItemsAsync(IClipboard clipboard)
    {
        /*
         * Only ask Avalonia for files.
         *
         * Do NOT call TryGetDataAsync() here.
         * It is unnecessary for detecting files and can cause
         * X11 clipboard request delays.
         */
        var rawData =
            await clipboard.TryGetFilesAsync();

        if (rawData == null)
            return null;

        var items = rawData.ToList();

        Console.WriteLine(
            $"Clipboard files: {items.Count}");

        foreach (var item in items)
            Console.WriteLine(
                $"  {item.Path}");

        return items;
    }

    private static List<string>? ExtractPaths(object? data)
    {
        if (data is IEnumerable<IStorageItem> storageItems)
            return storageItems
                .Select(item =>
                    item.Path.IsAbsoluteUri
                        ? item.Path.LocalPath
                        : item.Path.ToString())
                .ToList();

        if (data is IEnumerable<string> pathStrings) return pathStrings.ToList();

        return null;
    }
}