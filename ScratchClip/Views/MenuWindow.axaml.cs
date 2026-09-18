using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Timers;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using AvaloniaEdit.Utils;
using ScratchClip.Manager;
using ScratchClip.Models;
using ScratchClip.Services;
using ScratchClip.ViewModels;


namespace ScratchClip.Views;

public partial class MenuWindow : Window
{
    private readonly GlobalHotkeyService? _hotkeyService;
    private readonly ListViewModel _listViewModel;
    private readonly Timer _debounceTimer = new Timer(600);
    private int? _indexNumber;

    private ListBox? HistoryListBoxControl =>
        this.MenuViewModeControl
            .GetVisualDescendants()
            .OfType<ListBox>()
            .FirstOrDefault(x => x.Name == "ListBox");

    public MenuWindow(GlobalHotkeyService? hotkeyService)
    {
        _hotkeyService = hotkeyService;
        InitializeComponent();
        this.Deactivated += OnWindowDeactivated;
        _listViewModel = new ListViewModel
        {
            FilteredHistory = new ObservableCollection<AClipboardItem>()
        };
        MenuViewModeControl.DataContext = _listViewModel;

        ClipboardManager.OnClipboardItemAdded += (item) => { Search(); };
        ClipboardManager.OnRemoveExistingClipboardItem += (item) => { Search(); };
        ClipboardManager.OnClearExistingClipboardItem += () => { Search(); };

        ClipboardManager.OnDoubleTappedExistingClipboardItem += OnDoubleTappedExistingClipboardItem;
        _listViewModel.SelectedItem = ClipboardManager.SelectedClipboardItem;
        AddHandler(KeyDownEvent, OnWindowKeyDown, RoutingStrategies.Tunnel);
        _debounceTimer.Elapsed += DebounceTimerOnElapsed;
        Search();
    }


    private void OnWindowKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (_listViewModel.SelectedItem != null)
            {
                _ = ClipboardManager.SetClipboardItemAsync(_listViewModel.SelectedItem);
                this.Close();
                _ = _hotkeyService?.SimulatePasteAsync();
            }
        }
        else if (e.Key == Key.Up || e.Key == Key.Down)
        {
            var listBox = HistoryListBoxControl;
            if (listBox == null)
                return;
            Application.Current?.Dispatcher.Post(() =>
            {
                listBox.Focus();
                if (listBox.SelectedItem == null && listBox.SelectedIndex < 0 && listBox.ItemCount > 0)
                {
                    listBox.SelectedIndex = 0;
                }

                if (listBox.SelectedItem != null)
                {
                    var container = listBox.ContainerFromItem(listBox.SelectedItem);

                    container?.Focus(
                        NavigationMethod.Tab);
                }
            });
        }
        else if (e.Key == Key.Escape)
        {
            this.Close();
        }
        else
        {
            int? number = e.Key switch
            {
                >= Key.D0 and <= Key.D9 => (int)e.Key - (int)Key.D0,
                >= Key.NumPad0 and <= Key.NumPad9 => (int)e.Key - (int)Key.NumPad0,
                _ => null
            };
            if (number.HasValue)
            {
                _indexNumber = int.Parse($"{_indexNumber}{number}");
                _debounceTimer.Stop();
                _debounceTimer.Start();
                return;
            }

            _indexNumber = null;
        }

        SearchTextBox.Focus();
    }

    private async void DebounceTimerOnElapsed(object? sender, ElapsedEventArgs e)
    {
        _debounceTimer.Stop();
        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            if (HistoryListBoxControl == null)
                return;
            if (_indexNumber.HasValue)
            {
                var index = _indexNumber.Value - 1;
                if (index < 0)
                    index = 9;
                if (index < _listViewModel.FilteredHistory.Count)
                {
                    HistoryListBoxControl.SelectedItem =
                        _listViewModel.FilteredHistory.FirstOrDefault(x => x.DisplayIndex == _indexNumber.Value);
                    if (HistoryListBoxControl.SelectedItem is AClipboardItem item)
                        OnDoubleTappedExistingClipboardItem(item);
                }

                _indexNumber = null;
            }
        });
    }

    private async void OnDoubleTappedExistingClipboardItem(AClipboardItem obj)
    {
        await ClipboardManager.SetClipboardItemAsync(obj);
        this.Close();
        await _hotkeyService?.SimulatePasteAsync();
    }

    private void OnWindowDeactivated(object? sender, EventArgs e)
    {
        this.Close();
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        ClipboardManager.OnDoubleTappedExistingClipboardItem -= OnDoubleTappedExistingClipboardItem;
        base.OnClosing(e);
    }

    private void SearchTextBoxOnTextChanged(object? sender, TextChangedEventArgs e)
    {
        Search();
    }

    private void Search()
    {
        _listViewModel.FilteredHistory.Clear();
        _listViewModel.FilteredHistory.AddRange(ClipboardManager.ClipboardHistory
            .OrderByDescending(item => item.Timestamp)
            .Where(item =>
                item.Text.Contains(SearchTextBox.Text ?? string.Empty, StringComparison.OrdinalIgnoreCase)));
        UpdateDisplayIndexes();
    }


    private void UpdateDisplayIndexes()
    {
        if (SettingsManager.Load().IsReverseOrder)
        {
            for (var i = 0; i < _listViewModel.FilteredHistory.Count; i++)
                _listViewModel.FilteredHistory[i].DisplayIndex = i + 1;
            return;
        }

        for (var i = _listViewModel.FilteredHistory.Count - 1; i >= 0; i--)
            _listViewModel.FilteredHistory[i].DisplayIndex = _listViewModel.FilteredHistory.Count - i;
    }
}