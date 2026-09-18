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
using Avalonia.Input;
using Avalonia.Threading;
using AvaloniaEdit.Utils;
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
    private readonly Timer _timer;
    private readonly Timer _monitorTimer;

    private CancellationToken _token;

    private readonly List<AClipboardItem> _historyItems = new();

    public Action? OnHideToTray;
    public Action? OnOpenSettings;
    public Action? OnShowLockPage;
    public Action<AClipboardItem>? OnShowEditPage;
    public Action? OnListSetFocus;

    public Action<bool>? OnTopMostChanged;
    private bool _isUpdatingTagOptions;
    private CancellationTokenSource? _monitorCts;
    private string _registerNumber = string.Empty;
    private readonly ListViewModel _listViewModel;

    private readonly SemaphoreSlim _clipboardCheckLock = new(1, 1);

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
        ClipboardManager.OnDoubleTappedExistingClipboardItem += OnDoubleTappedExistingClipboardItem;
        foreach (var item in ClipboardManager.GetClipboardHistorySnapshot())
            _historyItems.Add(item);

        TagFilterOptions.CollectionChanged += OnTagFilterOptionsCollectionChanged;
        RefreshList();
        _searchDebounceTimer = new Timer(800);
        _searchDebounceTimer.Stop();
        _searchDebounceTimer.Elapsed += SearchDebounceTimerOnElapsed;
        _timer = new Timer(500);
        _timer.Elapsed += ClipboardTimerCallback;
        _timer.Stop();
        _monitorTimer = new Timer(2000);
        _monitorTimer.Elapsed += MonitorTimerCallback;
        _monitorTimer.Stop();
        StartMonitoringClipboard();
    }

    private async void OnDoubleTappedExistingClipboardItem(AClipboardItem obj)
    {
        OnHideToTray?.Invoke();
        await CopyAsync(obj);
        await _hotkeyService.SimulatePasteAsync();
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
            ClipboardManager.SelectedClipboardItem = value;
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
            NotificationHelper.Info(
                value ? "Pinned" : "Unpinned",
                value
                    ? "The application window is now pinned on top."
                    : "The application window is no longer pinned on top.");
            SettingsManager.Save(appSettings);
        }
    }

    public bool IsSearchFocused
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
        StopMonitoringClipboard(false);

        _monitorCts = new CancellationTokenSource();
        _token = _monitorCts.Token;

        _timer.Start();
        _monitorTimer.Start();
        NotificationHelper.Info(
            "Monitoring Active",
            "The clipboard monitoring has been successfully activated.");
    }

    private void MonitorTimerCallback(object? sender, ElapsedEventArgs e)
    {
        if (_token.IsCancellationRequested || !IsMonitoringClipboard)
        {
            _timer.Stop();
            _monitorTimer.Stop();
            return;
        }

        _timer.Start();
    }

    private async void ClipboardTimerCallback(
        object? sender,
        ElapsedEventArgs e)
    {
        if (_token.IsCancellationRequested || !IsMonitoringClipboard)
            return;

        if (!await _clipboardCheckLock.WaitAsync(0))
            return;

        try
        {
            var clipboardTask = Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (ClipboardManager.IsCheckingClipboard)
                    return Task.CompletedTask;
                return ClipboardManager.CheckClipboard();
            });

            var completed = await Task.WhenAny(
                clipboardTask,
                Task.Delay(TimeSpan.FromSeconds(3)));

            if (completed == clipboardTask)
            {
                await clipboardTask;
            }
            else
            {
                ClipboardManager.IsCheckingClipboard = false;
                Console.WriteLine("Clipboard check exceeded 3 seconds.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
        finally
        {
            _clipboardCheckLock.Release();
        }
    }

    private void StopMonitoringClipboard(bool showNotification = true)
    {
        _monitorCts?.Cancel();
        _monitorCts?.Dispose();
        _monitorCts = null;
        if (showNotification)
        {
            NotificationHelper.Info(
                "Monitoring Stopped",
                "The clipboard monitoring has been successfully stopped.");
        }
    }

    private void OnClipboardItemAdded(AClipboardItem clipboardItem)
    {
        _historyItems.Insert(0, clipboardItem);
        RefreshList();
        SelectedItem = clipboardItem;
    }

    private void OnSelectExistingClipboardItem(AClipboardItem clipboardItem)
    {
        SelectedItem = clipboardItem;
    }

    private void OnRemoveExistingClipboardItem(AClipboardItem item)
    {
        _historyItems.Remove(item);
        RefreshList();
    }

    public void RefreshList()
    {
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
        _historyItems.AddRange(ClipboardManager.ClipboardHistory);
        FilteredHistory.Clear();
        FilteredHistory.AddRange(ClipboardManager.ClipboardHistory);
        ApplyFilter();
        RefreshTagFilterOptions();
    }


    [RelayCommand]
    public void Lock()
    {
        var clipboardHistory = ClipboardManager.GetClipboardHistorySnapshot();
        ClipboardHistoryManager.Save(clipboardHistory, ApplicationKeyStore.GetSessionPassword());
        ApplicationKeyStore.ClearSessionPassword();
        ClipboardManager.ClearClipboardHistory();
        ClipboardManager.IsLoaded = false;
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

        SelectedItem = targetItem;
    }

    [RelayCommand]
    private void ClearHistory()
    {
        ClipboardManager.ClearClipboardHistory();
    }

    [RelayCommand]
    private async Task ClearClipboardAsync()
    {
        SelectedItem = null;
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
        if (FilteredHistory.All(q => q.Signature != SelectedItem?.Signature))
            SelectedItem = FilteredHistory.FirstOrDefault();

        UpdateDisplayIndexes();
    }

    private void ShowEditButtons()
    {
        foreach (var item in ClipboardManager.ClipboardHistory)
        {
            item.IsEditButtonVisible = true;
        }
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
        var note = item.Note ?? string.Empty;

        var bestScore = Math.Max(
            Fuzz.PartialRatio(query.ToLower(), text.ToLower()),
            Fuzz.PartialRatio(query.ToLower(), note.ToLower()));

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
        if (IsSearchFocused)
        {
            _searchDebounceTimer.Stop();
            return;
        }

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
            try
            {
                var item = FilteredHistory.FirstOrDefault(q => q.DisplayIndex == int.Parse(_registerNumber));
                if (item == null)
                {
                    _registerNumber = string.Empty;
                    return;
                }

                ClipboardManager.SelectedClipboardItem = item;
                OnListSetFocus?.Invoke();
                if (SettingsManager.Load().IsFastKeyEnabled)
                {
                    OnHideToTray?.Invoke();
                    await _hotkeyService.SimulatePasteAsync();
                }

                _registerNumber = string.Empty;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                _registerNumber = string.Empty;
            }
        }
    }

    public void WindowActivated()
    {
        ShowEditButtons();
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