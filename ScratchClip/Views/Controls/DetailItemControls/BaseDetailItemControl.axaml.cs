using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using ScratchClip.Models;

namespace ScratchClip.Views.Controls.DetailItemControls;

public partial class BaseDetailItemControl : UserControl
{
    public static readonly StyledProperty<Control?> ItemContentProperty =
        AvaloniaProperty.Register<BaseDetailItemControl, Control?>(
            nameof(ItemContent));

    private AClipboardItem? _clipboardItem;

    public BaseDetailItemControl()
    {
        InitializeComponent();

        DataContextChanged += OnDataContextChanged;
    }

    public Control? ItemContent
    {
        get => GetValue(ItemContentProperty);
        set => SetValue(ItemContentProperty, value);
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
            ControlButtonsPanel.IsVisible = _clipboardItem.IsSelected;
        }
    }

    private void OnClipboardItemPropertyChanged(
        object? sender,
        PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(AClipboardItem.IsSelected))
        {
            ControlButtonsPanel.IsVisible = _clipboardItem?.IsSelected == true;
        }
    }

    private async void OnDoubleTapped(object? sender, RoutedEventArgs e)
    {
        if (DataContext is AClipboardItem clipboardItem)
            await clipboardItem.OnDoubleTappedAsync();

        e.Handled = true;
    }

    private void OnPointerEntered(object? sender, PointerEventArgs e)
    {
        ControlButtonsPanel.IsVisible = true;
    }

    private void OnPointerExited(object? sender, PointerEventArgs e)
    {
        if (DataContext is AClipboardItem clipboardItem &&
            clipboardItem.IsSelected)
            return;

        ControlButtonsPanel.IsVisible = false;
    }
}