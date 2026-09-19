using System;
using Avalonia.Controls;
using AvaloniaEdit;
using AvaloniaEdit.Highlighting;
using ScratchClip.Models;
using ScratchClip.Models.TextType;

namespace ScratchClip.Views.Controls.TextTypeControls;

public partial class PlainTextTypeControl : UserControl
{
    private readonly TextEditor? _previewEditor;

    public PlainTextTypeControl()
    {
        InitializeComponent();
        _previewEditor = this.FindControl<TextEditor>("PreviewEditor");
        DataContextChanged += OnDataContextChanged;
        UpdatePreviewText();
        PreviewEditor.TextChanged += AdjustEditorHeight;
        PreviewEditor.SizeChanged += (s, e) => AdjustEditorHeight(s, EventArgs.Empty);
    }

    private void AdjustEditorHeight(object? sender, EventArgs e)
    {
        if (PreviewEditor.Document == null) return;

        // Estimate height based on line count, font size, padding, and line spacing
        double lineHeight = PreviewEditor.FontSize * 1.35; // Standard font line-height ratio
        double padding = PreviewEditor.Padding.Top + PreviewEditor.Padding.Bottom;

        // Total height needed by actual text content
        double contentHeight = (PreviewEditor.Document.LineCount * lineHeight) + padding + 20; // Additional buffer for scrollbar and margins

        // Cap height between content fit and max limit (400)
        PreviewEditor.Height = Math.Min(contentHeight, 400);
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        UpdatePreviewText();
    }

    private void UpdatePreviewText()
    {
        if (_previewEditor == null) return;

        _previewEditor.Text = DataContext is ATextType textType
            ? textType.Text
            : string.Empty;
    }

    private void ApplyGrammarForCurrentType()
    {
        if (_previewEditor == null) return;

        var definitionName = DataContext switch
        {
            JsonTextType => "JAVASCRIPT",
            XmlTextType => "XML",
            MarkdownTextType => "MARKDOWN",
            CodeTextType => "C#",
            _ => string.Empty
        };

        _previewEditor.SyntaxHighlighting = string.IsNullOrEmpty(definitionName)
            ? null
            : HighlightingManager.Instance.GetDefinition(definitionName);
    }
}