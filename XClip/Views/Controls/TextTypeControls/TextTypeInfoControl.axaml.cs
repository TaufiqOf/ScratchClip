using Avalonia.Controls;
using AvaloniaEdit;
using AvaloniaEdit.Highlighting;
using XClip.Models;
using XClip.Models.TextType;

namespace XClip.Views.Controls.TextTypeControls;

public partial class TextTypeInfoControl : UserControl
{
    private TextEditor? _previewEditor;

    public TextTypeInfoControl()
    {
        InitializeComponent();
        _previewEditor = this.FindControl<TextEditor>("PreviewEditor");
        DataContextChanged += OnDataContextChanged;
        UpdatePreviewText();
    }

    private void OnDataContextChanged(object? sender, System.EventArgs e)
    {
        UpdatePreviewText();
    }

    private void UpdatePreviewText()
    {
        if (_previewEditor == null) return;

        _previewEditor.Text = DataContext is ATextType textType
            ? textType.Text
            : string.Empty;
        ApplyGrammarForCurrentType();
    }

    private void ApplyGrammarForCurrentType()
    {
        if (_previewEditor == null) return;

        var definitionName = DataContext switch
        {
            JsonTextType => "JavaScript",
            XmlTextType => "XML",
            MarkdownTextType => "Markdown",
            CodeTextType => "C#",
            _ => string.Empty
        };

        _previewEditor.SyntaxHighlighting = string.IsNullOrEmpty(definitionName)
            ? null
            : HighlightingManager.Instance.GetDefinition(definitionName);


    }
}

