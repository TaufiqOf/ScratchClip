using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Avalonia.Input;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using FuzzySharp;
using XClip.Manager;
using XClip.Models;
using XClip.Services;
using Timer = System.Timers.Timer;

namespace XClip.ViewModels;

public partial class MainViewModel : ViewModelBase, IDisposable
{
    private const int FuzzyThreshold = 60;

    private readonly GlobalHotkeyService _hotkeyService;
    private readonly Timer _searchDebounceTimer;
    private readonly List<AClipboardItem> _historyItems = new();

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

        foreach (var item in ClipboardManager.GetClipboardHistorySnapshot())
            _historyItems.Add(item);

        ApplyFilter();

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
                ApplyFilter();
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

    private void OnClipboardItemAdded(AClipboardItem clipboardItem)
    {
        _historyItems.Insert(0, clipboardItem);
        ApplyFilter();
    }

    private void OnSelectExistingClipboardItem(AClipboardItem clipboardItem)
    {
        SelectedItem = clipboardItem;
    }

    private void OnRemoveExistingClipboardItem(AClipboardItem item)
    {
        _historyItems.Remove(item);
        ApplyFilter();
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
    private void ClearSearch()
    {
        SearchText = string.Empty;
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
        _historyItems.Clear();
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

    private void ApplyFilter()
    {
        IEnumerable<AClipboardItem> filteredItems;

        if (string.IsNullOrWhiteSpace(SearchText))
        {
            filteredItems = _historyItems;
        }
        else
        {
            var query = SearchText.Trim();
            filteredItems = _historyItems
                .Select(item => new
                {
                    Item = item,
                    Score = GetFuzzyScore(query, item)
                })
                .Where(x => x.Score >= FuzzyThreshold)
                .OrderByDescending(x => x.Score)
                .ThenByDescending(x => x.Item.Timestamp)
                .Select(x => x.Item);
        }

        FilteredHistory.Clear();
        foreach (var item in filteredItems.OrderByDescending(x => x.Timestamp))
            FilteredHistory.Add(item);

        UpdateDisplayIndexes();
        
        
    }

    private static int GetFuzzyScore(string query, AClipboardItem item)
    {
        var text = item.Text ?? string.Empty;
        var display = item.DisplayText ?? string.Empty;
        var bestScore = Math.Max(
            Fuzz.PartialRatio(query, text),
            Fuzz.PartialRatio(query, display));

        if (item is TextClipboardItem textItem)
        {
            if (textItem.TextType is WebsiteTextType websiteTextType)
            {
                bestScore = Math.Max(bestScore, Fuzz.PartialRatio(query, websiteTextType.WebsiteTitle ?? string.Empty));
                bestScore = Math.Max(bestScore, Fuzz.PartialRatio(query, websiteTextType.WebsiteDescription ?? string.Empty));
                bestScore = Math.Max(bestScore, Fuzz.PartialRatio(query, websiteTextType.WebsiteHost ?? string.Empty));
            }
        }

        return bestScore;
    }

    private void UpdateDisplayIndexes()
    {
        for (var i = FilteredHistory.Count - 1; i >= 0; i--)
            FilteredHistory[i].DisplayIndex = FilteredHistory.Count - i;
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
            _registerNumber += number.Value;
            _searchDebounceTimer.Stop();
            _searchDebounceTimer.Start();
        }
    }

    private async void SearchDebounceTimerOnElapsed(object? sender, ElapsedEventArgs e)
    {
        _searchDebounceTimer.Stop();
        Dispatcher.UIThread.Post(() => { _ = FastKeyExecute(); }, DispatcherPriority.Background);
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