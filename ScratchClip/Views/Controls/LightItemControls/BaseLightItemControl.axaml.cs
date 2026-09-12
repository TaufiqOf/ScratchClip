using Avalonia;
using Avalonia.Controls;

namespace ScratchClip.Views.Controls.LightItemControls;

public partial class BaseLightItemControl : UserControl
{
    public static readonly StyledProperty<Control?> ItemContentProperty =
        AvaloniaProperty.Register<BaseLightItemControl, Control?>(
            nameof(ItemContent));

    public Control? ItemContent
    {
        get => GetValue(ItemContentProperty);
        set => SetValue(ItemContentProperty, value);
    }


    public static readonly StyledProperty<Control?> ExtraActionsProperty =
        AvaloniaProperty.Register<BaseLightItemControl, Control?>(
            nameof(ExtraActions));

    public Control? ExtraActions
    {
        get => GetValue(ExtraActionsProperty);
        set => SetValue(ExtraActionsProperty, value);
    }


    public BaseLightItemControl()
    {
        InitializeComponent();
    }
}