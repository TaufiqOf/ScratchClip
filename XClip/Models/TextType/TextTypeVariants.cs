using System;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using FluentIcons.Common;

namespace XClip.Models.TextType;

public class CodeTextType : ATextType
{
    public CodeTextType(string text) : base(text)
    {
        Icon = Icon.CodeBlock;
        Text = text.Substring(0, Math.Min(text.Length, 600));  
    }

    public override string DisplayName => "Code";



    public override bool IsMatch(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var trimmed = text.Trim();

        if (Regex.IsMatch(trimmed,
                @"^\s*(public|private|protected|internal)\s+(class|interface|struct|enum)\b|^\s*(if|for|foreach|while|switch|try|catch)\s*\(",
                RegexOptions.IgnoreCase | RegexOptions.Multiline))
            return true;

        var keywordCount = Regex.Matches(
            trimmed,
            @"\b(class|interface|struct|enum|namespace|public|private|protected|internal|using|return|function|def|let|const|var|import|export|async|await|new|void|static)\b",
            RegexOptions.IgnoreCase).Count;

        var strongSymbolCount = Regex.Matches(trimmed, @"(=>|==|!=|<=|>=|\{|\}|\[|\]|;)").Count;
        var operatorCount = Regex.Matches(trimmed, @"(=>|==|!=|<=|>=|&&|\|\||\+\+|--)" ).Count;
        var isMultiline = trimmed.Contains('\n');

        // Treat as code only when there are multiple matching signals.
        if (isMultiline && strongSymbolCount >= 2 && keywordCount >= 1)
            return true;

        if (keywordCount >= 2 && (strongSymbolCount >= 1 || operatorCount >= 1))
            return true;

        return false;
    }

    public override Task PopulateMetadataAsync(string text)
    {
        return Task.CompletedTask;
    }
}

public class XmlTextType : ATextType
{
    public XmlTextType(string text) : base(text)
    {
        Icon = Icon.Markdown;
    }

    public override string DisplayName => "XML";

    public override bool IsMatch(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        try
        {
            _ = XDocument.Parse(text.Trim());
            return true;
        }
        catch
        {
            return false;
        }
    }

    public override Task PopulateMetadataAsync(string text)
    {
        return Task.CompletedTask;
    }
}

public class JsonTextType : ATextType
{
    public JsonTextType(string text) : base(text)
    {
        Icon = Icon.Markdown;
    }
    public override string DisplayName => "JSON";

    public override bool IsMatch(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        try
        {
            using var _ = JsonDocument.Parse(text.Trim());
            return true;
        }
        catch
        {
            return false;
        }
    }

    public override Task PopulateMetadataAsync(string text)
    {
        return Task.CompletedTask;
    }
}

public class MarkdownTextType : ATextType
{
    public MarkdownTextType(string text) : base(text)
    {
        Icon = Icon.Markdown;
    }
    public override string DisplayName => "Markdown";

    public override bool IsMatch(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var trimmed = text.Trim();

        return Regex.IsMatch(trimmed, @"(?m)^(#{1,6}\s+\S+|[-*+]\s+\S+)")
               || trimmed.Contains("```")
               || trimmed.Contains("**")
               || trimmed.Contains("__")
               || Regex.IsMatch(trimmed, @"\[[^\]]+\]\([^)]+\)");
    }

    public override Task PopulateMetadataAsync(string text)
    {
        return Task.CompletedTask;
    }
}