using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

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
        if (TopLevel.GetTopLevel(this) is MainWindow window)
            await window.ActivateSelectedItemAsync();

        e.Handled = true;
    }
}