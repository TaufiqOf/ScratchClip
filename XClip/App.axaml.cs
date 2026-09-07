using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Threading;
using XClip.Manager;
using XClip.Models;
using XClip.Services;
using XClip.Views;

namespace XClip;

public class App : Application
{
    private const string PipeName = "XClip_IPC_Pipe";
    private const int MaxRootItems = 50;
    private const int MaxItemsPerTag = 50;
    private GlobalHotkeyService? _hotkeyService;
    private bool _isCleanedUp;

    private TrayIcon? _trayIcon;

    public bool IsShuttingDown { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var args = desktop.Args ?? Array.Empty<string>();
            var isToggleRequested = args.Contains("--toggle-window", StringComparer.OrdinalIgnoreCase);

            // Check if a primary instance is already running
            if (CanConnectToExistingInstance(isToggleRequested))
            {
                // Exit the secondary process immediately before Avalonia starts its MainLoop
                Environment.Exit(0);
                return;
            }

            // --- PRIMARY INSTANCE SETUP ---
            var settings = SettingsManager.Load();

            if (settings.IsSaveHistoryOnExitEnabled)
            {
                var persistedItems = ClipboardHistoryManager.Load();
                ClipboardManager.LoadClipboardHistory(persistedItems);
            }

            _hotkeyService = new GlobalHotkeyService(ToggleMainWindow)
            {
                TargetModifiers = settings.Modifiers,
                TargetKey = settings.Key
            };

            if (_hotkeyService.IsSupported) _hotkeyService.Start();

            var mainWindow = new MainWindow(_hotkeyService);
            desktop.MainWindow = mainWindow;

            desktop.ShutdownRequested += OnShutdownRequested;
            desktop.Exit += OnDesktopExit;

            // Start background IPC server listener
            _ = StartIpcListenerAsync();

            if (isToggleRequested) Dispatcher.UIThread.Post(ToggleMainWindow);
        }

        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;

        var trayIcons = TrayIcon.GetIcons(this);
        if (trayIcons != null && trayIcons.Count > 0)
        {
            _trayIcon = trayIcons[0];
            _trayIcon.ToolTipText = "XClip";
            RebuildTrayMenu();
        }

        ClipboardManager.OnClipboardItemAdded += OnClipboardHistoryChanged;
        ClipboardManager.OnRemoveExistingClipboardItem += OnClipboardHistoryChanged;

        UpdateIcons(ActualThemeVariant);
        ActualThemeVariantChanged += OnActualThemeVariantChanged;

        base.OnFrameworkInitializationCompleted();
    }

    private static bool CanConnectToExistingInstance(bool isToggleRequested)
    {
        try
        {
            using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
            client.Connect(150); // Short timeout check

            if (isToggleRequested)
            {
                using var writer = new StreamWriter(client);
                writer.WriteLine("--toggle-window");
                writer.Flush();
            }

            return true; // Connection succeeded -> Secondary instance
        }
        catch
        {
            return false; // Connection failed -> Primary instance
        }
    }


    private async Task StartIpcListenerAsync()
    {
        while (!IsShuttingDown)
            try
            {
                using var server = new NamedPipeServerStream(
                    PipeName,
                    PipeDirection.In,
                    1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);

                await server.WaitForConnectionAsync();

                using var reader = new StreamReader(server);
                var message = await reader.ReadLineAsync();

                if (message == "--toggle-window") Dispatcher.UIThread.Post(ToggleMainWindow);
            }
            catch
            {
                // Ignore pipe interrupts during application shutdown
            }
    }

    private void OnActualThemeVariantChanged(object? sender, EventArgs e)
    {
        UpdateIcons(ActualThemeVariant);
    }

    private void UpdateIcons(ThemeVariant theme)
    {
        var assetUri = theme == ThemeVariant.Dark
            ? "avares://XClip/Assets/icon-dark.ico"
            : "avares://XClip/Assets/icon-light.ico";

        var uri = new Uri(assetUri);

        Dispatcher.UIThread.Post(() =>
        {
            if (_trayIcon != null)
            {
                using var trayStream = AssetLoader.Open(uri);
                _trayIcon.Icon = new WindowIcon(trayStream);
            }

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
            {
                using var windowStream = AssetLoader.Open(uri);
                desktop.MainWindow.Icon = new WindowIcon(windowStream);
            }
        });
    }

    private async void TrayIcon_OnClicked(object? sender, EventArgs e)
    {
        // var item = ClipboardManager.SelectedClipboardItem ?? ClipboardManager.GetClipboardHistorySnapshot().FirstOrDefault();
        // if (item != null)
        // {
        //     await PasteItemToFocusedWindowAsync(item);
        //     return;
        // }

        ToggleMainWindow();
    }

    private void ShowApp_OnClick(object? sender, EventArgs e)
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop &&
            desktop.MainWindow is MainWindow window)
            window.ShowFromTray();
    }

    private void ExitApp_OnClick(object? sender, EventArgs e)
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            IsShuttingDown = true;
            if (desktop.MainWindow is MainWindow window) window.ForceExit();

            CleanupResources();
            desktop.Shutdown();
        }
    }

    private void ToggleMainWindow()
    {
        if (IsShuttingDown) return;

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop &&
            desktop.MainWindow is MainWindow window)
        {
            if (window is { IsVisible: true, IsActive: true })
                window.HideToTray();
            else
                window.ShowFromTray();
        }
    }

    private void OnShutdownRequested(object? sender, ShutdownRequestedEventArgs e)
    {
        IsShuttingDown = true;

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop &&
            desktop.MainWindow is MainWindow window)
            window.ForceExit();

        CleanupResources();
    }

    private void OnDesktopExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        IsShuttingDown = true;
        CleanupResources();
        AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;
    }

    private void OnProcessExit(object? sender, EventArgs e)
    {
        IsShuttingDown = true;
        CleanupResources();
    }

    private void CleanupResources()
    {
        if (_isCleanedUp) return;

        _isCleanedUp = true;

        var settings = SettingsManager.Load();
        if (settings.IsSaveHistoryOnExitEnabled)
        {
            var history = ClipboardManager.GetClipboardHistorySnapshot();
            ClipboardHistoryManager.Save(history);
        }

        ActualThemeVariantChanged -= OnActualThemeVariantChanged;
        ClipboardManager.OnClipboardItemAdded -= OnClipboardHistoryChanged;
        ClipboardManager.OnRemoveExistingClipboardItem -= OnClipboardHistoryChanged;
        _hotkeyService?.Dispose();
        _hotkeyService = null;
    }

    private void OnClipboardHistoryChanged(AClipboardItem _)
    {
        Dispatcher.UIThread.Post(RebuildTrayMenu);
    }

    private void RebuildTrayMenu()
    {
        if (_trayIcon == null)
            return;

        var history = ClipboardManager.GetClipboardHistorySnapshot().OrderByDescending(x => x.Timestamp).ToList();
        var rootMenu = new NativeMenu();

        var showAppItem = new NativeMenuItem("Show App");
        showAppItem.Click += ShowApp_OnClick;
        rootMenu.Items.Add(showAppItem);
        rootMenu.Items.Add(new NativeMenuItemSeparator());

        if (history.Count == 0)
        {
            rootMenu.Items.Add(new NativeMenuItem("No clipboard items") { IsEnabled = false });
        }
        else
        {
            for (var i = 0; i < Math.Min(MaxRootItems, history.Count); i++)
            {
                var clipboardItem = history[i];
                var menuItem = new NativeMenuItem(BuildItemHeader(i + 1, clipboardItem));
                menuItem.Click += async (_, _) => await PasteItemToFocusedWindowAsync(clipboardItem);
                rootMenu.Items.Add(menuItem);
            }
        }

        rootMenu.Items.Add(new NativeMenuItemSeparator());

        var tagsRoot = BuildTagsMenu(history);
        rootMenu.Items.Add(tagsRoot);

        rootMenu.Items.Add(new NativeMenuItemSeparator());
        var exitItem = new NativeMenuItem("Exit");
        exitItem.Click += ExitApp_OnClick;
        rootMenu.Items.Add(exitItem);

        _trayIcon.Menu = rootMenu;
    }

    private NativeMenuItem BuildTagsMenu(IReadOnlyList<AClipboardItem> history)
    {
        var tagsMenu = new NativeMenuItem("Tags")
        {
            Menu = new NativeMenu()
        };

        var groupedByTag = history
            .SelectMany(item => item.Tags
                .Where(tag => !string.IsNullOrWhiteSpace(tag))
                .Select(tag => new { Tag = tag.Trim(), Item = item }))
            .GroupBy(x => x.Tag, StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (groupedByTag.Count == 0)
        {
            tagsMenu.Menu!.Items.Add(new NativeMenuItem("No tags") { IsEnabled = false });
            return tagsMenu;
        }

        for (var i = 0; i < groupedByTag.Count; i++)
        {
            var group = groupedByTag[i];
            var tagMenu = new NativeMenuItem(BuildNumberedHeader(i + 1, group.Key))
            {
                Menu = new NativeMenu()
            };

            var items = group
                .Select(x => x.Item)
                .Distinct()
                .OrderByDescending(x => x.Timestamp)
                .Take(MaxItemsPerTag)
                .ToList();

            for (var itemIndex = 0; itemIndex < items.Count; itemIndex++)
            {
                var clipboardItem = items[itemIndex];
                var subMenuItem = new NativeMenuItem(BuildItemHeader(itemIndex + 1, clipboardItem));
                subMenuItem.Click += async (_, _) => await PasteItemToFocusedWindowAsync(clipboardItem);
                tagMenu.Menu!.Items.Add(subMenuItem);
            }

            tagsMenu.Menu!.Items.Add(tagMenu);
        }

        return tagsMenu;
    }

    private async Task PasteItemToFocusedWindowAsync(AClipboardItem item)
    {
        await ClipboardManager.SetClipboardItemAsync(item);
        await Task.Delay(120);

        if (_hotkeyService is { IsSupported: true })
            await _hotkeyService.SimulatePasteAsync();
    }

    private static string BuildItemHeader(int index, AClipboardItem item)
    {
        var text = string.IsNullOrWhiteSpace(item.DisplayText) ? item.Text : item.DisplayText;
        var singleLine = text.Replace('\n', ' ').Replace('\r', ' ').Trim();
        var preview = singleLine.Length > 60 ? singleLine[..60] + "..." : singleLine;
        return BuildNumberedHeader(index, preview);
    }

    private static string BuildNumberedHeader(int index, string label)
    {
        if (index is >= 1 and <= 9)
            return $"_{index}. {label}";

        return $"{index}. {label}";
    }

}