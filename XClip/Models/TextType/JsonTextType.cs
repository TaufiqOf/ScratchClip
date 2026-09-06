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