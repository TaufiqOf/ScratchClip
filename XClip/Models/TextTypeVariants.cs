using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace XClip.Models;

public class CodeTextType : ATextType
{
    public override string DisplayName => "Code";

    public override bool IsMatch(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var trimmed = text.Trim();

        if (trimmed.Contains('\n') && (trimmed.Contains('{') || trimmed.Contains(';') || trimmed.Contains("=>")))
            return true;

        return Regex.IsMatch(
            trimmed,
            @"\b(class|interface|struct|enum|namespace|public|private|protected|internal|using|return|function|def|let|const|var|import|export)\b",
            RegexOptions.IgnoreCase);
    }

    public override Task PopulateMetadataAsync(string text)
    {
        return Task.CompletedTask;
    }
}

public class XmlTextType : ATextType
{
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



