using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using Avalonia.Xaml.Interactions.Core;
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

    [ObservableProperty] private string _hotkeyDisplay;
    [ObservableProperty] private string _hotkeyMenuDisplay;

    [ObservableProperty] private int _maximumItemsInHistory;
    [ObservableProperty] private string? _exportFilePath;
    private bool _willExportTextItems = true;
    private bool _willExportImageItems = true;
    private bool _willExportStorageItems = true;

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


    [RelayCommand]
    public void Export()
    {
        if (string.IsNullOrEmpty(ExportFilePath))
        {
            return;
        }

        var clipboardHistory = ClipboardManager.GetClipboardHistorySnapshot()
            .Where(q => (WillExportTextItems && q.ClipboardType == ClipboardType.Text)
                        || (WillExportImageItems && q.ClipboardType == ClipboardType.Image)
                        || (WillExportStorageItems && q.ClipboardType == ClipboardType.Storage)).ToList();
    }

    [RelayCommand]
    public async Task SelectFile()
    {
        var storageProvider = ApplicationReference.MainWindow?.StorageProvider;
        if (storageProvider == null)
        {
            return;
        }

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

        var files = await storageProvider.SaveFilePickerWithResultAsync(new FilePickerSaveOptions()
        {
            Title = "Select a file",
            FileTypeChoices = filePickerTypes
        });
        if (files.File?.Path != null)
        {
            var file = files.File.Path;
            if (file != null)
            {
                ExportFilePath = file.AbsolutePath;
            }
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

    [RelayCommand]
    private void Save()
    {
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
        settings.Theme = SelectedTheme;
        ClipboardManager.MaxItemsInHistory = settings.MaxItemsInHistory;
        SettingsManager.Save(settings);
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