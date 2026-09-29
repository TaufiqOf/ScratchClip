using System;
using System.ComponentModel;
using System.Timers;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using ScratchClip.Models;

namespace ScratchClip.Views.Controls.MenuItemControls;

public partial class BaseMenuItemControl : UserControl
{
    public static readonly StyledProperty<Control?> ItemContentProperty =
        AvaloniaProperty.Register<BaseMenuItemControl, Control?>(
            nameof(ItemContent));

    private AClipboardItem? _clipboardItem;
    private readonly Timer _timer;
    private bool _isPointerOver;

    public BaseMenuItemControl()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        _timer = new System.Timers.Timer(200);
        _timer.Elapsed += TimerOnElapsed;
    }

    private void TimerOnElapsed(object? sender, ElapsedEventArgs e)
    {
        _timer.Stop();
        Application.Current?.Dispatcher.Invoke(() => { TagsItemsControl.IsVisible = _isPointerOver; });
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_clipboardItem is not null)
        {
            _clipboardItem.PropertyChanged -= OnClipboardItemPropertyChanged;
        }

        _clipboardItem = DataContext as AClipboardItem;

        if (_clipboardItem is not null)
        {
            _clipboardItem.PropertyChanged += OnClipboardItemPropertyChanged;

            // Optional: initialize immediately
            TagsItemsControl.IsVisible = _clipboardItem.IsSelected;
        }
    }

    private void OnClipboardItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AClipboardItem.IsSelected))
        {
            TagsItemsControl.IsVisible = _clipboardItem?.IsSelected ?? false;
        }
    }

    public Control? ItemContent
    {
        get => GetValue(ItemContentProperty);
        set => SetValue(ItemContentProperty, value);
    }

    private async void OnDoubleTapped(object? sender, RoutedEventArgs e)
    {
        if (DataContext is AClipboardItem clipboardItem) await clipboardItem.OnDoubleTappedAsync();

        e.Handled = true;
    }

    private void OnPointerEntered(object? sender, PointerEventArgs e)
    {
        _timer.Stop();
        _timer.Start();
        _isPointerOver = true;
    }

    private void OnPointerExited(object? sender, PointerEventArgs e)
    {
        if (DataContext is AClipboardItem clipboardItem &&
            clipboardItem.IsSelected)
            return;
        _timer.Stop();
        _timer.Start();
        _isPointerOver = false;
    }
}