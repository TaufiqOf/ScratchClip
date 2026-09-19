using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScratchClip.Helper;
using ScratchClip.Manager;
using ScratchClip.Models;
using ScratchClip.Services;
using SharpHook.Data;

namespace ScratchClip.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly GlobalHotkeyService _hotkeyService;
    [ObservableProperty] private string? _exportFilePath;

    [ObservableProperty] private string _hotkeyDisplay;
    [ObservableProperty] private string _hotkeyMenuDisplay;

    [ObservableProperty] private int _maximumItemsInHistory;
    private bool _willExportImageItems = true;
    private bool _willExportStorageItems = true;
    private bool _willExportTextItems = true;
    private bool _willCaptureImageItems; 
    private bool _willCaptureTextItems;
    private bool _willCaptureStorageItems;

    public SettingsViewModel(GlobalHotkeyService hotkeyService)
    {
        _hotkeyService = hotkeyService;

        // Initialize with current settings
        var settings = SettingsManager.Load();
        PendingModifiers = _hotkeyService.TargetModifiers;
        PendingKey = _hotkeyService.TargetKey;
        PendingMenuModifiers = _hotkeyService.MenuTargetModifiers;
        PendingMenuKey = _hotkeyService.MenuTargetKey;
        HotkeyDisplay = $"{PendingModifiers} + {PendingKey}".Replace("Left", "").Replace("Right", "");
        HotkeyMenuDisplay = $"{PendingMenuModifiers} + {PendingMenuKey}".Replace("Left", "").Replace("Right", "");
        IsAutoStartEnabled = AutoStartManager.IsEnabled();
        IsSaveHistoryOnExitEnabled = settings.IsSaveHistoryOnExitEnabled;
        MaximumItemsInHistory = settings.MaxItemsInHistory;
        IsFastKeyEnabled = settings.IsFastKeyEnabled;
        IsReverseOrder = settings.IsReverseOrder;
        SelectedTheme = settings.Theme;
        WillCaptureImageItems = settings.WillCaptureImageItems;
        WillCaptureTextItems = settings.WillCaptureTextItems;
        WillCaptureStorageItems = settings.WillCaptureStorageItems;
        SelectedTheme = settings.Theme switch
        {
            "Light" => "Light",
            "Dark" => "Dark",
            _ => "System"
        };
    }

    public bool IsReverseOrder
    {
        get;
        set
        {
            if (value == field) return;
            field = value;
            OnPropertyChanged();
        }
    }

    public bool IsAutoStartEnabled
    {
        get;
        set
        {
            AutoStartManager.SetEnabled(value);
            SetProperty(ref field, value);
        }
    }

    public bool IsSaveHistoryOnExitEnabled
    {
        get;
        set => SetProperty(ref field, value);
    }

    public EventMask PendingModifiers { get; private set; }
    public KeyCode PendingKey { get; private set; }
    public EventMask PendingMenuModifiers { get; set; }
    public KeyCode PendingMenuKey { get; set; }

    public string SelectedTheme
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
            }
        }
    }

    public bool IsFastKeyEnabled
    {
        get;
        set => SetProperty(ref field, value);
    }

    public bool WillExportTextItems
    {
        get => _willExportTextItems;
        set
        {
            if (value == _willExportTextItems) return;
            _willExportTextItems = value;
            ExportFilePath = string.Empty;
            OnPropertyChanged();
        }
    }

    public bool WillExportImageItems
    {
        get => _willExportImageItems;
        set
        {
            if (value == _willExportImageItems) return;
            _willExportImageItems = value;
            ExportFilePath = string.Empty;
            OnPropertyChanged();
        }
    }

    public bool WillExportStorageItems
    {
        get => _willExportStorageItems;
        set
        {
            if (value == _willExportStorageItems) return;
            _willExportStorageItems = value;
            ExportFilePath = string.Empty;
            OnPropertyChanged();
        }
    }

    public bool WillCaptureTextItems
    {
        get => _willCaptureTextItems;
        set { SetProperty(ref _willCaptureTextItems, value); }
    }

    public bool WillCaptureImageItems
    {
        get => _willCaptureImageItems;
        set { SetProperty(ref _willCaptureImageItems, value); }
    }

    public bool WillCaptureStorageItems
    {
        get => _willCaptureStorageItems;
        set { SetProperty(ref _willCaptureStorageItems, value); }
    }



    [RelayCommand]
    public void Export()
    {
        try
        {
            if (string.IsNullOrEmpty(ExportFilePath)) return;

            var clipboardHistory = ClipboardManager.GetClipboardHistorySnapshot()
                .Where(q => (WillExportTextItems && q.ClipboardType == ClipboardType.Text)
                            || (WillExportImageItems && q.ClipboardType == ClipboardType.Image)
                            || (WillExportStorageItems && q.ClipboardType == ClipboardType.Storage)).ToList();
            var finalePath = Path.GetDirectoryName(ExportFilePath);

            if (finalePath == null) return;

            if (WillExportImageItems && WillExportStorageItems)
                finalePath = Path.Combine(finalePath, Path.GetFileNameWithoutExtension(ExportFilePath));

            if (!Directory.Exists(finalePath)) Directory.CreateDirectory(finalePath);

            if (WillExportTextItems)
            {
                var textItems = clipboardHistory
                    .Where(q => q.ClipboardType == ClipboardType.Text).Cast<TextClipboardItem>()
                    .ToList();
                if (textItems.Any())
                {
                    var i = 1;
                    var stringBuilder = new StringBuilder();
                    foreach (var item in textItems)
                    {
                        stringBuilder.AppendLine($"Item {i}:");
                        stringBuilder.AppendLine(item.Text);
                        stringBuilder.AppendLine("------------------------------");
                        i++;
                    }

                    var textFilePath = Path.Combine(finalePath, "TextItems.txt");
                    File.WriteAllText(textFilePath, stringBuilder.ToString());
                }
            }

            if (WillExportImageItems)
            {
                var imageItems = clipboardHistory
                    .Where(q => q.ClipboardType == ClipboardType.Image).Cast<ImageClipboardItem>()
                    .ToList();
                if (imageItems.Any())
                {
                    var i = 1;
                    foreach (var imageClipboardItem in imageItems)
                    {
                        // Save each image to a separate file
                        var imageFilePath = Path.Combine(finalePath, $"ImageItem_{i}.png");
                        imageClipboardItem.Image?.Save(imageFilePath, new PngBitmapEncoderOptions());
                        i++;
                    }
                }
            }

            if (WillExportStorageItems)
            {
                var storageItems = clipboardHistory
                    .Where(q => q.ClipboardType == ClipboardType.Storage).Cast<StorageClipboardItem>()
                    .ToList();
                if (storageItems.Any())
                {
                    var i = 1;
                    foreach (var storageClipboardItem in storageItems)
                    {
                        // Save each storage item to a separate file
                        foreach (var file in storageClipboardItem.Files)
                        {
                            var storageFilePath = Path.Combine(finalePath, $"StorageItem_{i}",
                                $"{Path.GetFileName(file)}");
                            if (!Directory.Exists(Path.GetDirectoryName(storageFilePath)))
                                Directory.CreateDirectory(Path.GetDirectoryName(storageFilePath));

                            if (File.Exists(file))
                                try
                                {
                                    File.Copy(file, storageFilePath, true);
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine(e);
                                }
                        }

                        foreach (var folder in storageClipboardItem.Folders)
                        {
                            var folderName = new DirectoryInfo(folder).Name;

                            var storageFolderPath = Path.Combine(
                                finalePath,
                                $"StorageItem_{i}",
                                folderName);
                            if (Directory.Exists(folder))
                            {
                                if (!Directory.Exists(storageFolderPath)) Directory.CreateDirectory(storageFolderPath);

                                CopyDirectory(folder, storageFolderPath);
                            }
                        }

                        i++;
                    }
                }
            }

            if (File.Exists(ExportFilePath)) File.Delete(ExportFilePath);

            if (WillExportImageItems || WillExportStorageItems)
            {
                ZipFile.CreateFromDirectory(
                    finalePath,
                    ExportFilePath,
                    CompressionLevel.Optimal,
                    false);
                Directory.Delete(finalePath, true);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    public static void CopyDirectory(
        string sourceDir,
        string destinationDir)
    {
        Directory.CreateDirectory(destinationDir);

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var destinationFile = Path.Combine(
                destinationDir,
                Path.GetFileName(file));
            try
            {
                File.Copy(file, destinationFile, true);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        foreach (var directory in Directory.GetDirectories(sourceDir))
        {
            var destinationSubdirectory = Path.Combine(
                destinationDir,
                Path.GetFileName(directory));

            CopyDirectory(directory, destinationSubdirectory);
        }
    }

    [RelayCommand]
    public async Task SelectFile()
    {
        var storageProvider = ApplicationReference.MainWindow?.StorageProvider;
        if (storageProvider == null) return;

        var filePickerTypes = new List<FilePickerFileType>();
        if (WillExportImageItems || WillExportStorageItems)
        {
            filePickerTypes.Clear();
            filePickerTypes.Add(new FilePickerFileType("Zip Files")
            {
                Patterns = new List<string> { "*.zip" }
            });
            filePickerTypes.Add(new FilePickerFileType("All Files")
            {
                Patterns = new List<string> { "*.*" }
            });
        }
        else
        {
            filePickerTypes.Clear();
            filePickerTypes.Add(new FilePickerFileType("Text Files")
            {
                Patterns = new List<string> { "*.txt" }
            });
            filePickerTypes.Add(new FilePickerFileType("All Files")
            {
                Patterns = new List<string> { "*.*" }
            });
        }

        var files = await storageProvider.SaveFilePickerWithResultAsync(new FilePickerSaveOptions
        {
            Title = "Select a file",
            FileTypeChoices = filePickerTypes
        });
        if (files.File?.Path != null)
        {
            var file = files.File.Path;
            if (file != null) ExportFilePath = file.AbsolutePath;
        }
    }

    [RelayCommand]
    private void ClearHotkey()
    {
        PendingModifiers = EventMask.LeftAlt | EventMask.LeftShift;
        PendingKey = KeyCode.VcK;
        HotkeyDisplay = "Alt + Shift + K";
    }

    [RelayCommand]
    private void ClearHotkeyMenu()
    {
        PendingMenuModifiers = EventMask.LeftAlt | EventMask.LeftShift;
        PendingMenuKey = KeyCode.VcL;
        HotkeyMenuDisplay = "Alt + Shift + L";
    }

    public bool Save()
    {
        if (!WillCaptureImageItems && !WillCaptureTextItems && !WillCaptureStorageItems)
        {
            NotificationHelper.Error("Settings Error","At least one clipboard type must be enabled for capture.");
            return false;
        }

        // 1. Update active runtime hotkey configuration
        _hotkeyService.UpdateHotkey(PendingModifiers, PendingKey);
        _hotkeyService.UpdateMenuHotkey(PendingMenuModifiers, PendingMenuKey);
        Application.Current?.RequestedThemeVariant = SelectedTheme?.ToLowerInvariant() switch
        {
            "light" => ThemeVariant.Light,
            "dark" => ThemeVariant.Dark,
            _ => ThemeVariant.Default // "System" or default
        };
        // 2. Persist to disk
        var settings = SettingsManager.Load();
        settings.IsSaveHistoryOnExitEnabled = IsSaveHistoryOnExitEnabled;

        settings.Modifiers = PendingModifiers;
        settings.Key = PendingKey;
        settings.MenuModifiers = PendingMenuModifiers;
        settings.MenuKey = PendingMenuKey;

        settings.IsAutoStartEnabled = AutoStartManager.IsEnabled();
        settings.MaxItemsInHistory = MaximumItemsInHistory;
        settings.IsReverseOrder = IsReverseOrder;
        settings.IsFastKeyEnabled = IsFastKeyEnabled;
        settings.WillCaptureImageItems = WillCaptureImageItems;
        settings.WillCaptureTextItems = WillCaptureTextItems;
        settings.WillCaptureStorageItems = WillCaptureStorageItems;
        settings.Theme = SelectedTheme;
        ClipboardManager.MaxItemsInHistory = settings.MaxItemsInHistory;
        SettingsManager.Save(settings);
        return true;
    }

    public void SetMenuHotkey(EventMask modifiers, KeyCode key, string display)
    {
        PendingMenuModifiers = modifiers;
        PendingMenuKey = key;
        HotkeyMenuDisplay = display;
    }


    public void SetHotkey(EventMask modifiers, KeyCode key, string display)
    {
        PendingModifiers = modifiers;
        PendingKey = key;
        HotkeyDisplay = display;
    }
}