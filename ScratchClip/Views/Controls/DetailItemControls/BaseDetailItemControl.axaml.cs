using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ScratchClip.Models;

namespace ScratchClip.Views.Controls.DetailItemControls;

public partial class BaseDetailItemControl : UserControl
{
    public static readonly StyledProperty<Control?> ItemContentProperty =
        AvaloniaProperty.Register<BaseDetailItemControl, Control?>(
            nameof(ItemContent));

    public Control? ItemContent
    {
        get => GetValue(ItemContentProperty);
        set => SetValue(ItemContentProperty, value);
    }

    public BaseDetailItemControl()
    {
        InitializeComponent();
    }
    // private async void OnDoubleTapped(object? sender, RoutedEventArgs e)
    // {
    //     if (TopLevel.GetTopLevel(this) is MainWindow window)
    //         await window.ActivateSelectedItemAsync();
    //
    //     e.Handled = true;
    // }
    private async void OnDoubleTapped(object? sender, RoutedEventArgs e)
    {
        if(DataContext is AClipboardItem clipboardItem)
        {
            await clipboardItem.OnDoubleTappedAsync();
        }

        e.Handled = true;
    }
}