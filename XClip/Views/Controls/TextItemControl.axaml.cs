using Avalonia.Controls;
using Avalonia.Interactivity;
using XClip.Views;

namespace XClip.Views.Controls;

public partial class TextItemControl : UserControl
{
    public TextItemControl()
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