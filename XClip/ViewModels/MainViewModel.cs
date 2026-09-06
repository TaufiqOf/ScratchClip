using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Avalonia.Input;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using XClip.Manager;
using XClip.Models;
using XClip.Services;
using Timer = System.Timers.Timer;

namespace XClip.ViewModels;

public partial class MainViewModel : ViewModelBase, IDisposable
{
    private readonly GlobalHotkeyService _hotkeyService;
    private readonly Timer _searchDebounceTimer;
    public Action? OnHideToTray;
    public Action? OnOpenSettings;
    private bool _isInternalSelectionChange;
    private CancellationTokenSource? _monitorCts;
    private string _registerNumber = string.Empty;

    public MainViewModel(GlobalHotkeyService hotkeyService)
    {
        _hotkeyService = hotkeyService;
        ClipboardManager.OnClipboardItemAdded += OnClipboardItemAdded;
        ClipboardManager.OnSelectExistingClipboardItem += OnSelectExistingClipboardItem;
        ClipboardManager.OnRemoveExistingClipboardItem += OnRemoveExistingClipboardItem;

        foreach (var item in ClipboardManager.GetClipboardHistorySnapshot().OrderBy(q => q.Timestamp))
            OnClipboardItemAdded(item);

        StartMonitoringClipboard();
        _searchDebounceTimer = new Timer(300);
        _searchDebounceTimer.Stop();
        _searchDebounceTimer.Elapsed += SearchDebounceTimerOnElapsed;
    }

    public ObservableCollection<AClipboardItem> FilteredHistory { get; } = new();


    public string SearchText
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
            }
        }
    } = string.Empty;


    public bool IsMonitoringClipboard
    {
        get;
        set
        {
            SetProperty(ref field, value);
            if (value)
                StartMonitoringClipboard();
            else
                StopMonitoringClipboard();
        }
    } = true;

    public AClipboardItem? SelectedItem
    {
        get;
        set
        {
            if (!SetProperty(ref field, value) || value == null)
                return;

            if (_isInternalSelectionChange)
                return;
            _ = SetClipboardItemAsync(value);
        }
    }

    public void Dispose()
    {
        StopMonitoringClipboard();
    }

    public async Task SimulatePasteAsync()
    {
        await _hotkeyService.SimulatePasteAsync();
    }

    private void StartMonitoringClipboard()
    {
        StopMonitoringClipboard();
        _monitorCts = new CancellationTokenSource();
        _ = MonitorClipboardAsync(_monitorCts.Token);
    }

    private void StopMonitoringClipboard()
    {
        _monitorCts?.Cancel();
        _monitorCts?.Dispose();
        _monitorCts = null;
    }

    private void OnClipboardItemAdded(AClipboardItem textClipboardItem)
    {
        textClipboardItem.DisplayIndex = FilteredHistory.Count + 1;
        FilteredHistory.Insert(0, textClipboardItem);
    }

    private void OnSelectExistingClipboardItem(AClipboardItem textClipboardItem)
    {
        SelectedItem = textClipboardItem;
    }

    private void OnRemoveExistingClipboardItem(AClipboardItem obj)
    {
        FilteredHistory.Remove(obj);
    }


    private async Task MonitorClipboardAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(500));

        while (!cancellationToken.IsCancellationRequested && await timer.WaitForNextTickAsync(cancellationToken))
            await Dispatcher.UIThread.InvokeAsync(() => _ = ClipboardManager.CheckClipboard());
    }


    public async Task DoubleClickAsync()
    {
        await CopyAsync(SelectedItem);
    }

    [RelayCommand]
    private async Task CopyAsync(AClipboardItem? item)
    {
        var targetItem = item ?? SelectedItem;
        if (targetItem == null) return;

        await SetClipboardItemAsync(targetItem);

        _isInternalSelectionChange = true;
        SelectedItem = targetItem;
        _isInternalSelectionChange = false;
    }

    [RelayCommand]
    private void ClearHistory()
    {
        ClipboardManager.ClearClipboardHistory();
        FilteredHistory.Clear();
    }

    [RelayCommand]
    private async Task ClearClipboardAsync()
    {
        _isInternalSelectionChange = true;
        SelectedItem = null;
        _isInternalSelectionChange = false;
        await ClipboardManager.ClearClipboardData();
    }

    [RelayCommand]
    private Task OpenSettingsAsync()
    {
        OnOpenSettings?.Invoke();
        return Task.CompletedTask;
    }


    private async Task SetClipboardItemAsync(AClipboardItem targetItem)
    {
        await ClipboardManager.SetClipboardItemAsync(targetItem);
    }


    public void OnWindowKeyDown(KeyEventArgs keyEventArgs)
    {
        int? number = keyEventArgs.Key switch
        {
            >= Key.D1 and <= Key.D9 => (int)keyEventArgs.Key - (int)Key.D1 + 1,
            >= Key.NumPad1 and <= Key.NumPad9 => (int)keyEventArgs.Key - (int)Key.NumPad1 + 1,
            _ => null
        };

        if (number.HasValue)
        {
            _registerNumber += number.Value; // Or number.Value.ToString() if registerNumber is a string
            _searchDebounceTimer.Stop();
            _searchDebounceTimer.Start();
        }
    }


    private async void SearchDebounceTimerOnElapsed(object? sender, ElapsedEventArgs e)
    {
        _searchDebounceTimer.Stop();
        Dispatcher.UIThread.Post(() =>
        {
            _ = FastKeyExecute(); // Fire-and-forget safely or handle exceptions inside FastKeyExecute
        }, DispatcherPriority.Background);
    }

    private async Task FastKeyExecute()
    {
        if (!string.IsNullOrEmpty(_registerNumber))
        {
            var item = FilteredHistory.FirstOrDefault(q => q.DisplayIndex == int.Parse(_registerNumber));
            if (item != null) ClipboardManager.SelectedClipboardItem = item;

            OnHideToTray?.Invoke();
            await _hotkeyService.SimulatePasteAsync();

            _registerNumber = string.Empty;
        }
    }
}