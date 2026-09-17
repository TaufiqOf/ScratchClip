using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ScratchClip.Models;

namespace ScratchClip.Views.Controls.MenuItemControls;

public partial class BaseMenuItemControl : UserControl
{
    
    public static readonly StyledProperty<Control?> ItemContentProperty =
        AvaloniaProperty.Register<BaseMenuItemControl, Control?>(
            nameof(ItemContent));

    public Control? ItemContent
    {
        get => GetValue(ItemContentProperty);
        set => SetValue(ItemContentProperty, value);
    }

    public BaseMenuItemControl()
    {
        InitializeComponent();
    }
    private async void OnDoubleTapped(object? sender, RoutedEventArgs e)
    {
        if(DataContext is AClipboardItem clipboardItem)
        {
            await clipboardItem.OnDoubleTappedAsync();
        }

        e.Handled = true;
    }
}