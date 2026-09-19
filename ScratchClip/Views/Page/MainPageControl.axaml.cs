using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Styling;
using ScratchClip.Helper;
using ScratchClip.ViewModels;

namespace ScratchClip.Views.Page;

public partial class MainPageControl : UserControl
{
    public MainPageControl()
    {
        InitializeComponent();

        OnPasswordChanged();
        SearchTextBox.KeyUp += SearchTextBoxOnKeyUp;
        ApplicationKeyStore.OnPasswordChanged += OnPasswordChanged;
        ApplicationTheme.OnThemeChanged += OnThemeChanged;
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        OnThemeChanged(ApplicationReference.MainWindow.ActualThemeVariant);
    }

    private void OnPasswordChanged()
    {
        var lockButtonIsVisible = ApplicationKeyStore.HasPassword();
        LockButton.IsVisible = lockButtonIsVisible;
    }

    private void OnThemeChanged(ThemeVariant obj)
    {
        var uri = new Uri(ApplicationTheme.GetIcon(obj, "png","32x32"));

        using var trayStream = AssetLoader.Open(uri);

        using var memoryStream = new MemoryStream();

        trayStream.CopyTo(memoryStream);
        memoryStream.Position = 0;

        LogoImage.Source = new Bitmap(memoryStream);
    }

    private void SearchTextBoxOnGotFocus(object? sender, FocusChangedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel) viewModel.IsSearchFocused = true;
    }

    private void SearchTextBoxOnLostFocus(object? sender, FocusChangedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel) viewModel.IsSearchFocused = false;
    }

    private void SearchTextBoxOnKeyUp(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Down)
            if (DataContext is MainViewModel viewModel)
                viewModel.OnListSetFocus?.Invoke();
    }
}