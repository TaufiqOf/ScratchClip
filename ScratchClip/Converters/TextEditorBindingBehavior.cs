using System;
using Avalonia;
using Avalonia.Xaml.Interactivity;
using AvaloniaEdit;

namespace ScratchClip.Converters;

public class TextEditorBindingBehavior : Behavior<TextEditor>
{
    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<TextEditorBindingBehavior, string?>(
            nameof(Text));

    private bool _updating;

    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();

        if (AssociatedObject == null)
            return;

        AssociatedObject.TextChanged += OnEditorTextChanged;

        // Initial VM -> editor
        AssociatedObject.Text = Text ?? string.Empty;
    }

    protected override void OnDetaching()
    {
        if (AssociatedObject != null)
            AssociatedObject.TextChanged -= OnEditorTextChanged;

        base.OnDetaching();
    }

    private void OnEditorTextChanged(object? sender, EventArgs e)
    {
        if (_updating || AssociatedObject == null)
            return;

        _updating = true;

        try
        {
            Text = AssociatedObject.Text;
        }
        finally
        {
            _updating = false;
        }
    }

    protected override void OnPropertyChanged(
        AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property != TextProperty ||
            AssociatedObject == null ||
            _updating)
            return;

        _updating = true;

        try
        {
            AssociatedObject.Text = Text ?? string.Empty;
        }
        finally
        {
            _updating = false;
        }
    }
}