using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using FuzzySharp;
using ScratchClip.Helper;
using ScratchClip.Manager;
using ScratchClip.Models;
using ScratchClip.Services;
using ScratchClip.Views.Controls;
using Timer = System.Timers.Timer;
using WebsiteTextType = ScratchClip.Models.TextType.WebsiteTextType;

namespace ScratchClip.ViewModels;

public partial class MainViewModel : ViewModelBase, IDisposable
{
    private const int FuzzyThreshold = 60;

    private readonly GlobalHotkeyService _hotkeyService;
    private readonly Timer _searchDebounceTimer;
    private readonly List<AClipboardItem> _historyItems = new();

    public Action? OnHideToTray;
    public Action? OnOpenSettings;
    public Action? OnShowLockPage;
    public Action<AClipboardItem>? OnShowEditPage;
    
    public Action<bool>? OnTopMostChanged;
    private bool _isInternalSelectionChange;
    private bool _isUpdatingTagOptions;
    private CancellationTokenSource? _monitorCts;
    private string _registerNumber = string.Empty;
    private readonly ListViewModel _listViewModel;

    public MainViewModel(GlobalHotkeyService hotkeyService)
    {
        _listViewModel = new ListViewModel()
        {
            FilteredHistory = FilteredHistory,
            SelectedItem = SelectedItem
        };

        _listViewModel.OnItemSelected += OnItemSelected;
        var appSettings = SettingsManager.Load();
        SettingsManager.OnSettingsUpdated += OnSettingsUpdated;
        SelectedMode = appSettings.ViewMode;
        IsPinned = appSettings.IsPinned;
        _hotkeyService = hotkeyService;
        ClipboardManager.OnClipboardItemAdded += OnClipboardItemAdded;
        ClipboardManager.OnSelectExistingClipboardItem += OnSelectExistingClipboardItem;
        ClipboardManager.OnRemoveExistingClipboardItem += OnRemoveExistingClipboardItem;
        ClipboardManager.OnClearExistingClipboardItem += OnClearExistingClipboardItem;
        ClipboardManager.OnEditExistingClipboardItem += OnEditExistingClipboardItem;
        foreach (var item in ClipboardManager.GetClipboardHistorySnapshot())
            _historyItems.Add(item);

        TagFilterOptions.CollectionChanged += OnTagFilterOptionsCollectionChanged;
        RefreshTagFilterOptions();
        ApplyFilter();

        StartMonitoringClipboard();
        _searchDebounceTimer = new Timer(800);
        _searchDebounceTimer.Stop();
        _searchDebounceTimer.Elapsed += SearchDebounceTimerOnElapsed;
    }



    private void OnSettingsUpdated(AppSettings obj)
    {
        UpdateDisplayIndexes();
    }


    public ObservableCollection<AClipboardItem> FilteredHistory { get; } = new();
    public ObservableCollection<TagFilterOption> TagFilterOptions { get; } = new();

    public UserControl? ListContent
    {
        get;
        set => SetProperty(ref field, value);
    }


    public List<ViewMode> ModeOptions => Enum.GetValues(typeof(ViewMode)).Cast<ViewMode>().ToList();

    public ViewMode SelectedMode
    {
        get => field;
        set
        {
            SetProperty(ref field, value);
            var appSettings = SettingsManager.Load();
            appSettings.ViewMode = value;
            SettingsManager.Save(appSettings);
            SetListContentControl(_listViewModel, value);
        }
    }

    public string SelectedTagsSummary
    {
        get
        {
            var selectedTags = GetSelectedTags();
            return selectedTags.Count switch
            {
                0 => "All tags",
                <= 4 => string.Join(", ", selectedTags),
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
            _listViewModel.SelectedItem = value;
            if (_isInternalSelectionChange)
                return;
            _ = SetClipboardItemAsync(value);
        }
    }

    public bool IsPinned
    {
        get => field;

        set
        {
            SetProperty(ref field, value);
            var appSettings = SettingsManager.Load();
            appSettings.IsPinned = value;
            OnTopMostChanged?.Invoke(value);
            SettingsManager.Save(appSettings);
        }
    }

    public bool IsPasswordSet
    {
        get => field;
        set
        {
            SetProperty(ref field, value);
            OnPropertyChanged();
        }
    }

    public void Dispose()
    {
        ClipboardManager.OnClipboardItemAdded -= OnClipboardItemAdded;
        ClipboardManager.OnSelectExistingClipboardItem -= OnSelectExistingClipboardItem;
        ClipboardManager.OnRemoveExistingClipboardItem -= OnRemoveExistingClipboardItem;
        ClipboardManager.OnClearExistingClipboardItem -= OnClearExistingClipboardItem;
        ClipboardManager.OnEditExistingClipboardItem -= OnEditExistingClipboardItem;
        TagFilterOptions.CollectionChanged -= OnTagFilterOptionsCollectionChanged;

        foreach (var option in TagFilterOptions)
            option.PropertyChanged -= OnTagOptionPropertyChanged;

        StopMonitoringClipboard();
    }

    private void SetListContentControl(ListViewModel listViewModel, ViewMode mode)
    {
        if (mode == ViewMode.Detailed)
        {
            ListContent = new ListDetailControl()
            {
                DataContext = listViewModel
            };
        }
        else if (mode == ViewMode.Compact)
        {
            ListContent = new ListLightControl()
            {
                DataContext = listViewModel
            };
        }
        else
        {
            ListContent = new MenuViewModeControl()
            {
                DataContext = listViewModel
            };
        }
    }

    private void OnItemSelected(AClipboardItem obj)
    {
        SelectedItem = obj;
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
        SelectedItem = clipboardItem;
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
    
    private void OnEditExistingClipboardItem(AClipboardItem obj)
    {
        OnShowEditPage?.Invoke(obj);
    }
    
    private void OnClearExistingClipboardItem()
    {
        _historyItems.Clear();
        FilteredHistory.Clear();
        RefreshTagFilterOptions();
    }

    private async Task MonitorClipboardAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(500));

        while (!cancellationToken.IsCancellationRequested && await timer.WaitForNextTickAsync(cancellationToken))
            await Dispatcher.UIThread.InvokeAsync(() => _ = ClipboardManager.CheckClipboard());
    }

    [RelayCommand]
    public void Lock()
    {
        OnShowLockPage?.Invoke();
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
        var text = item.Text;
        var display = item.DisplayText;

        var bestScore = Math.Max(
            Fuzz.PartialRatio(query, text),
            Fuzz.PartialRatio(query, display));

        if (item is TextClipboardItem { TextType: WebsiteTextType websiteTextType })
        {
            bestScore = Math.Max(bestScore, Fuzz.PartialRatio(query, websiteTextType.WebsiteTitle));
            bestScore = Math.Max(bestScore, Fuzz.PartialRatio(query, websiteTextType.WebsiteDescription));
            bestScore = Math.Max(bestScore, Fuzz.PartialRatio(query, websiteTextType.WebsiteHost));
        }

        return bestScore;
    }

    private void UpdateDisplayIndexes()
    {
        if (SettingsManager.Load().IsReverseOrder)
        {
            for (var i = 0; i < FilteredHistory.Count; i++)
                FilteredHistory[i].DisplayIndex = i + 1;
            return;
        }

        for (var i = FilteredHistory.Count - 1; i >= 0; i--)
            FilteredHistory[i].DisplayIndex = FilteredHistory.Count - i;
    }

    public void OnWindowKeyDown(KeyEventArgs keyEventArgs)
    {
        int? number = keyEventArgs.Key switch
        {
            >= Key.D0 and <= Key.D9 => (int)keyEventArgs.Key - (int)Key.D0,
            >= Key.NumPad0 and <= Key.NumPad9 => (int)keyEventArgs.Key - (int)Key.NumPad0,
            _ => null
        };

        if (number.HasValue)
        {
            _registerNumber += number.Value;
            Debug.WriteLine($"Number pressed: {number.Value}");
            Debug.WriteLine($"_registerNumber_registerNumber: {_registerNumber}");
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