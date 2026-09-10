using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScratchClip.Manager;
using ScratchClip.Services;
using SharpHook.Data;

namespace ScratchClip.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly GlobalHotkeyService _hotkeyService;

    [ObservableProperty] private string _hotkeyDisplay;
    [ObservableProperty] private int _maximumItemsInHistory;

    public SettingsViewModel(GlobalHotkeyService hotkeyService)
    {
        _hotkeyService = hotkeyService;

        // Initialize with current settings
        var settings = SettingsManager.Load();
        PendingModifiers = _hotkeyService.TargetModifiers;
        PendingKey = _hotkeyService.TargetKey;
        HotkeyDisplay = $"{PendingModifiers} + {PendingKey}".Replace("Left", "").Replace("Right", "");
        IsAutoStartEnabled = AutoStartManager.IsEnabled();
        IsSaveHistoryOnExitEnabled = settings.IsSaveHistoryOnExitEnabled;
        MaximumItemsInHistory = settings.MaxItemsInHistory;
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

    public void SetHotkey(EventMask modifiers, KeyCode key, string display)
    {
        PendingModifiers = modifiers;
        PendingKey = key;
        HotkeyDisplay = display;
    }

    [RelayCommand]
    private void ClearHotkey()
    {
        PendingModifiers = EventMask.LeftAlt | EventMask.LeftShift;
        PendingKey = KeyCode.VcK;
        HotkeyDisplay = "Alt + Shift + K";
    }

    [RelayCommand]
    private void Save()
    {
        // 1. Update active runtime hotkey configuration
        _hotkeyService.UpdateHotkey(PendingModifiers, PendingKey);

        // 2. Persist to disk
        var settings = SettingsManager.Load();
        settings.IsSaveHistoryOnExitEnabled = IsSaveHistoryOnExitEnabled;
        settings.Modifiers = PendingModifiers;
        settings.Key = PendingKey;
        settings.IsAutoStartEnabled = AutoStartManager.IsEnabled();
        settings.MaxItemsInHistory = MaximumItemsInHistory;
        SettingsManager.Save(settings);
    }
}