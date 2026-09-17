using System;
using System.Collections.ObjectModel;
using System.Linq;
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

// Required for RoutingStrategies

namespace ScratchClip.Views;

public partial class MenuWindow : Window
{
    private readonly GlobalHotkeyService? _hotkeyService;
    private readonly ListViewModel _listViewModel;

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
        ClipboardManager.OnClipboardItemAdded += (item) =>
        {
            Search();
        };
        ClipboardManager.OnRemoveExistingClipboardItem += (item) =>
        {
            Search();
        };
        ClipboardManager.OnClearExistingClipboardItem += () =>
        {
            Search();
        };
        ClipboardManager.OnDoubleTappedExistingClipboardItem += OnDoubleTappedExistingClipboardItem;
        _listViewModel.SelectedItem = ClipboardManager.SelectedClipboardItem;
        AddHandler(KeyDownEvent,  OnWindowKeyDown,RoutingStrategies.Tunnel);
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
            return;
        }

        if(e.Key == Key.Up || e.Key == Key.Down)
        {
            var listBox = HistoryListBoxControl;
            if(listBox == null)
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
            return;
        }
        SearchTextBox.Focus();
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

    private void SearchTextBoxOnTextChanged(object? sender, TextChangedEventArgs e)
    {
        Search();
    }

    private void Search()
    {
        
        _listViewModel.FilteredHistory.Clear();
        _listViewModel.FilteredHistory.AddRange(ClipboardManager.ClipboardHistory.OrderByDescending(item => item.Timestamp)
            .Where(item =>
            item.Text.Contains(SearchTextBox.Text ?? string.Empty, StringComparison.OrdinalIgnoreCase)));
        UpdateDisplayIndexes();
    }
    private ListBox? HistoryListBoxControl =>
        this.MenuViewModeControl
            .GetVisualDescendants()
            .OfType<ListBox>()
            .FirstOrDefault(x => x.Name == "ListBox");

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