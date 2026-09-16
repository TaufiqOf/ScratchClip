using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using ScratchClip.Models;

namespace ScratchClip.Views.Controls.StorageTypeControls;

public partial class StorageItemsViewerControl : UserControl
{
    public StorageItemsViewerControl()
    {
        InitializeComponent();
    }

    // ============================================================
    // OPEN
    // ============================================================

    private async void OpenFile_Click(
        object? sender,
        RoutedEventArgs e)
    {
        if (sender is not MenuItem
            {
                CommandParameter: StorageItem item
            })
            return;

        if (!Exists(item.FullPath))
            return;

        try
        {
            await RunCommand(
                "xdg-open",
                item.FullPath);
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"Failed to open '{item.FullPath}': {ex}");
        }
    }


    // ============================================================
    // COPY
    // ============================================================

    private async void CopyFile_Click(
        object? sender,
        RoutedEventArgs e)
    {
        if (sender is not MenuItem
            {
                CommandParameter: StorageItem item
            })
            return;

        if (!Exists(item.FullPath))
            return;

        var destination = await PickFolder();

        if (destination == null)
            return;

        try
        {
            await RunCommand(
                "gio",
                "copy",
                item.FullPath,
                destination);
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"Failed to copy '{item.FullPath}': {ex}");
        }
    }


    // ============================================================
    // MOVE
    // ============================================================

    private async void MoveFile_Click(
        object? sender,
        RoutedEventArgs e)
    {
        if (sender is not MenuItem
            {
                CommandParameter: StorageItem item
            })
            return;

        if (!Exists(item.FullPath))
            return;

        var destination = await PickFolder();

        if (destination == null)
            return;

        try
        {
            await RunCommand(
                "gio",
                "move",
                item.FullPath,
                destination);
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"Failed to move '{item.FullPath}': {ex}");
        }
    }


    // ============================================================
    // DELETE
    // ============================================================

    private async void DeleteFile_Click(
        object? sender,
        RoutedEventArgs e)
    {
        if (sender is not MenuItem
            {
                CommandParameter: StorageItem item
            })
            return;

        if (!Exists(item.FullPath))
            return;

        try
        {
            // Send to Linux Trash.
            //
            // This is safer than File.Delete() because the user
            // can restore the file from Trash.

            await RunCommand(
                "gio",
                "trash",
                item.FullPath);
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"Failed to delete '{item.FullPath}': {ex}");
        }
    }


    // ============================================================
    // CHECK FILE / DIRECTORY
    // ============================================================

    private static bool Exists(string path)
    {
        return File.Exists(path) ||
               Directory.Exists(path);
    }


    // ============================================================
    // FOLDER PICKER
    // ============================================================

    private async Task<string?> PickFolder()
    {
        var topLevel = TopLevel.GetTopLevel(this);

        if (topLevel?.StorageProvider == null)
            return null;

        var folders =
            await topLevel.StorageProvider.OpenFolderPickerAsync(
                new FolderPickerOpenOptions
                {
                    Title = "Select destination folder",
                    AllowMultiple = false
                });

        if (folders.Count == 0)
            return null;

        return folders[0].Path.LocalPath;
    }


    // ============================================================
    // RUN LINUX COMMAND
    // ============================================================

    private static async Task RunCommand(
        string command,
        params string[] arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = command,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo);

        if (process == null)
        {
            throw new InvalidOperationException(
                $"Could not start '{command}'.");
        }

        var errorTask =
            process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        var error = await errorTask;

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"{command} failed with exit code " +
                $"{process.ExitCode}: {error}");
        }
    }
}