using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Styling;
using Avalonia.Threading;
using ScratchClip.Helper;
using ScratchClip.Manager;
using ScratchClip.Models;
using ScratchClip.Services;
using ScratchClip.ViewModels;
using ScratchClip.Views.Page;

// Required for RoutingStrategies

namespace ScratchClip.Views;

public partial class MainWindow : Window
{
    private readonly GlobalHotkeyService _hotkeyService;
    private readonly MainPageControl _mainPage;
    private readonly MainViewModel _viewModel;
    private bool _isClosingForReal;
    private SettingPageControl? _settingsPage;


    public MainWindow() : this(new GlobalHotkeyService(() => { }))
    {
    }

    public MainWindow(GlobalHotkeyService hotkeyService)
    {
        _hotkeyService = hotkeyService;
        InitializeComponent();
        _viewModel = new MainViewModel(hotkeyService);
        _viewModel.OnHideToTray += HideToTray;
        _viewModel.OnOpenSettings += ShowSettingsPage;
        _viewModel.OnTopMostChanged += TopMostChanged;
        _viewModel.OnShowLockPage += ShowLockPage;
        _viewModel.OnShowEditPage += OnShowEditPage;
        DataContext = _viewModel;
        _mainPage = new MainPageControl(this.Icon);
        _mainPage.DataContext = _viewModel;

        if (ApplicationKeyStore.HasPassword())
        {
            ShowLockPage();
        }
        else
        {
            ShowMainPage();
        }

        Opened += OnOpened;
        Closed += OnClosed;
        Deactivated += OnWindowDeactivated;
        Activated += OnActivated;
        // Use Tunnel routing strategy to catch key presses before ListBox consumes them
        AddHandler(KeyDownEvent, OnWindowKeyDown, RoutingStrategies.Tunnel);
    }


    private void OnShowEditPage(AClipboardItem obj)
    {
        EditClipboardItemPageControl editPage = new EditClipboardItemPageControl(obj);
        PageHost.Content = editPage;
        editPage.OnClose += (AClipboardItem? item) =>
        {
            ShowMainPage();
            Dispatcher.UIThread.Post(FocusControls, DispatcherPriority.Input);
        };
    }


    private void ShowLockPage()
    {
        PasswordPageControl passwordPage = new PasswordPageControl();
        PageHost.Content = passwordPage;
        passwordPage.OnLogin += () =>
        {
            ShowMainPage();
            Dispatcher.UIThread.Post(FocusControls, DispatcherPriority.Input);
        };
    }

    private void TopMostChanged(bool isPinned)
    {
        Topmost = isPinned;
    }

    private TextBox? SearchTextBoxControl =>
        _mainPage.FindControl<TextBox>("SearchTextBox");

    private ListBox? HistoryListBox =>
        _mainPage.FindControl<ListBox>("ListBox");

    private void OnWindowDeactivated(object? sender, EventArgs e)
    {
        var settings = SettingsManager.Load();
        if (!settings.IsPinned && IsVisible)
            HideToTray();
        Dispatcher.UIThread.Post(() =>
        {
            var settings = SettingsManager.Load();
            settings.WindowWidth = Width;
            settings.WindowHeight = Height;
            settings.IsPinned = Topmost;
            SettingsManager.Save(settings);
        }, DispatcherPriority.Background);
    }
    
    
    private void OnActivated(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            var settings = SettingsManager.Load();
            Width = settings.WindowWidth;
            Height = settings.WindowHeight;
            TopMostChanged(settings.IsPinned);
        }, DispatcherPriority.Background);
    }

    private void OnWindowKeyDown(object? sender, KeyEventArgs e)
    {
        if (!ReferenceEquals(PageHost.Content, _mainPage))
            return;

        if (e.Key == Key.S && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            e.Handled = true;
            var searchBox = SearchTextBoxControl;
            searchBox?.Focus();
            searchBox?.SelectAll();
        }

        if (e.Key == Key.Escape)
        {
            e.Handled = true;
            HideToTray();
        }

        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            _ = ActivateSelectedItemAsync();
        }

        _viewModel.OnWindowKeyDown(e);
    }

    public async Task ActivateSelectedItemAsync()
    {
        HideToTray();
        await _viewModel.DoubleClickAsync();
        await _viewModel.SimulatePasteAsync();
    }


    private void OnOpened(object? sender, EventArgs e)
    {
        PositionInBottomRight();
    }

    private static bool IsWayland()
    {
        if (!OperatingSystem.IsLinux())
            return false;

        // Most reliable runtime hints
        var sessionType = Environment.GetEnvironmentVariable("XDG_SESSION_TYPE");
        var waylandDisplay = Environment.GetEnvironmentVariable("WAYLAND_DISPLAY");

        return string.Equals(sessionType, "wayland", StringComparison.OrdinalIgnoreCase)
               || !string.IsNullOrWhiteSpace(waylandDisplay);
    }

    private void PositionInBottomRight()
    {
        // Wayland compositors usually ignore app-requested absolute position.
        if (IsWayland())
            return;

        var screen = Screens.Primary;
        if (screen == null) return;

        var workArea = screen.WorkingArea;
        var windowWidthPixels = (int)(Width * screen.Scaling);
        var windowHeightPixels = (int)(Height * screen.Scaling);

        var x = workArea.X + workArea.Width - windowWidthPixels;
        var y = workArea.Y + workArea.Height - windowHeightPixels;

        Position = new PixelPoint(x, y);
    }


    public void ForceExit()
    {
        _isClosingForReal = true;
        Close();
    }

    public void HideToTray()
    {
        ShowInTaskbar = false;
        Hide();
    }

    public void ShowFromTray()
    {
        //ShowMainPage();
        ShowInTaskbar = true;
        Show();
        var settings = SettingsManager.Load();
        WindowState = WindowState.Normal;
        Width = settings.WindowWidth;
        Height = settings.WindowHeight;
        PositionInBottomRight();
        Activate();
        Dispatcher.UIThread.Post(FocusControls, DispatcherPriority.Input);
    }

    private void FocusControls()
    {
        var listBox = HistoryListBox;
        if (listBox == null)
            return;

        // Bring window to front natively
        if (WindowState == WindowState.Minimized) WindowState = WindowState.Normal;

        Activate();

        // Give the OS window manager a frame to settle activation before setting control focus
        Dispatcher.UIThread.Post(() =>
        {
            if (DataContext is MainViewModel vm && vm.FilteredHistory.Any())
            {
                if (listBox.SelectedIndex < 0)
                    listBox.SelectedIndex = 0;

                var container = listBox.ContainerFromIndex(listBox.SelectedIndex);
                if (container is Control control)
                    control.Focus();
                else
                    listBox.Focus();
            }
            else
            {
                listBox.Focus();
            }
        }, DispatcherPriority.Render);
    }

    private void ShowMainPage()
    {
        PageHost.Content = _mainPage;
    }

    private void ShowSettingsPage()
    {
        if (_settingsPage == null)
        {
            _settingsPage = new SettingPageControl();
            _settingsPage.CloseRequested += OnSettingsCloseRequested;
        }

        _settingsPage.DataContext = new SettingsViewModel(_hotkeyService);
        PageHost.Content = _settingsPage;
    }

    private void OnSettingsCloseRequested(object? sender, EventArgs e)
    {
        ShowMainPage();
        Dispatcher.UIThread.Post(FocusControls, DispatcherPriority.Input);
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _viewModel.OnHideToTray -= HideToTray;
        _viewModel.OnOpenSettings -= ShowSettingsPage;
        _viewModel.Dispose();
        if (_settingsPage != null)
            _settingsPage.CloseRequested -= OnSettingsCloseRequested;
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        var appIsShuttingDown = (Application.Current as App)?.IsShuttingDown == true;

        if (!_isClosingForReal && !appIsShuttingDown)
        {
            e.Cancel = true;
            HideToTray();
        }

        var settings = SettingsManager.Load();
        settings.WindowWidth = Width;
        settings.WindowHeight = Height;
        SettingsManager.Save(settings);
        base.OnClosing(e);
    }
}