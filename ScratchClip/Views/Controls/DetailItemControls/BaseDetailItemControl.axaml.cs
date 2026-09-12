using Avalonia;
using Avalonia.Controls;

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
}