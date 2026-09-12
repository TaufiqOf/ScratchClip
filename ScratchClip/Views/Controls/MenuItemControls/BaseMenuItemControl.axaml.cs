using Avalonia;
using Avalonia.Controls;

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
}