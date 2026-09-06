using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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
using WebsiteTextType = XClip.Models.TextType.WebsiteTextType;

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
    private bool _isUpdatingTagOptions;
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

        TagFilterOptions.CollectionChanged += OnTagFilterOptionsCollectionChanged;
        RefreshTagFilterOptions();
        ApplyFilter();

        StartMonitoringClipboard();
        _searchDebounceTimer = new Timer(300);
        _searchDebounceTimer.Stop();
        _searchDebounceTimer.Elapsed += SearchDebounceTimerOnElapsed;
    }

    public ObservableCollection<AClipboardItem> FilteredHistory { get; } = new();
    public ObservableCollection<TagFilterOption> TagFilterOptions { get; } = new();

    public string SelectedTagsSummary
    {
        get
        {
            var selectedTags = GetSelectedTags();
            return selectedTags.Count switch
            {
                0 => "All tags",
                <= 2 => string.Join(", ", selectedTags),
                _ => $"{selectedTags.Count} tags selected"
            };
        }
    }

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
        ClipboardManager.OnClipboardItemAdded -= OnClipboardItemAdded;
        ClipboardManager.OnSelectExistingClipboardItem -= OnSelectExistingClipboardItem;
        ClipboardManager.OnRemoveExistingClipboardItem -= OnRemoveExistingClipboardItem;
        TagFilterOptions.CollectionChanged -= OnTagFilterOptionsCollectionChanged;

        foreach (var option in TagFilterOptions)
            option.PropertyChanged -= OnTagOptionPropertyChanged;

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
        RefreshTagFilterOptions();
        ApplyFilter();
    }

    private void OnSelectExistingClipboardItem(AClipboardItem clipboardItem)
    {
        SelectedItem = clipboardItem;
    }

    private void OnRemoveExistingClipboardItem(AClipboardItem item)
    {
        _historyItems.Remove(item);
        RefreshTagFilterOptions();
        ApplyFilter();
    }

    private async Task MonitorClipboardAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(500));

        while (!cancellationToken.IsCancellationRequested && await timer.WaitForNextTickAsync(cancellationToken))
            await Dispatcher.UIThread.InvokeAsync(() => _ = ClipboardManager.CheckClipboard());
    }
    
    [RelayCommand]
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
    private void ClearTagFilter()
    {
        _isUpdatingTagOptions = true;
        foreach (var option in TagFilterOptions)
            option.IsSelected = false;
        _isUpdatingTagOptions = false;

        OnPropertyChanged(nameof(SelectedTagsSummary));
        ApplyFilter();
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
        RefreshTagFilterOptions();
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
        var selectedTags = GetSelectedTags();
        IEnumerable<AClipboardItem> filteredItems;

        if (string.IsNullOrWhiteSpace(SearchText))
        {
            filteredItems = _historyItems.Where(item => MatchesTagFilter(item, selectedTags));
        }
        else
        {
            var query = SearchText.Trim();
            filteredItems = _historyItems
                .Where(item => MatchesTagFilter(item, selectedTags))
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

    private static bool MatchesTagFilter(AClipboardItem item, IReadOnlySet<string> selectedTags)
    {
        if (selectedTags.Count == 0)
            return true;

        return item.Tags.Any(tag => selectedTags.Contains(tag));
    }

    private HashSet<string> GetSelectedTags()
    {
        return TagFilterOptions
            .Where(x => x.IsSelected)
            .Select(x => x.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private void RefreshTagFilterOptions()
    {
        var selectedTags = GetSelectedTags();
        var availableTags = _historyItems
            .SelectMany(item => item.Tags)
            .Select(tag => tag.Trim())
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(tag => tag, StringComparer.OrdinalIgnoreCase)
            .ToList();

        _isUpdatingTagOptions = true;
        TagFilterOptions.Clear();
        foreach (var tag in availableTags)
        {
            var option = new TagFilterOption(tag)
            {
                IsSelected = selectedTags.Contains(tag)
            };
            TagFilterOptions.Add(option);
        }

        _isUpdatingTagOptions = false;
        OnPropertyChanged(nameof(SelectedTagsSummary));
    }

    private void OnTagOptionPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(TagFilterOption.IsSelected) || _isUpdatingTagOptions)
            return;

        OnPropertyChanged(nameof(SelectedTagsSummary));
        ApplyFilter();
    }

    private void OnTagFilterOptionsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
        {
            foreach (TagFilterOption option in e.OldItems)
                option.PropertyChanged -= OnTagOptionPropertyChanged;
        }

        if (e.NewItems != null)
        {
            foreach (TagFilterOption option in e.NewItems)
                option.PropertyChanged += OnTagOptionPropertyChanged;
        }
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

public class TagFilterOption(string name) : ViewModelBase
{
    public string Name { get; } = name;

    public bool IsSelected
    {
        get;
        set => SetProperty(ref field, value);
    }
}

