using System.Collections.ObjectModel;
using System.Text.Json;
using System.Threading.Tasks;
using FluentIcons.Common;

namespace XClip.Models.TextType;

public class JsonTextType : ATextType
{
    public JsonTextType(string text, ObservableCollection<string> tags) : base(text, tags)
    {
        Icon = Icon.Markdown;
    }

    public override string DisplayName => "JSON";

    public override bool IsMatch(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var trimmed = text.Trim();

        // Treat only structured JSON as JSON type; scalars are valid JSON,
        // but they are usually plain text in clipboard usage.
        if (!(trimmed.StartsWith("{") || trimmed.StartsWith("[")))
            return false;

        try
        {
            using var _ = JsonDocument.Parse(trimmed);
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