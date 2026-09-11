using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FluentIcons.Common;

namespace ScratchClip.Models.TextType;

public class MarkdownTextType : ATextType
{
    public MarkdownTextType(string text, ObservableCollection<string> tags, List<string?> mataData) : base(text, tags)
    {
        Icon = Icon.Markdown;
    }

    public override string DisplayName => "Markdown";

    public override bool IsMatch(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var trimmed = text.Trim();

        return Regex.IsMatch(
                   trimmed,
                   @"(?m)^(#{1,6}\s+\S+|[-*+]\s+\S+)")
               || trimmed.Contains("```")
               || trimmed.Contains("**")
               || trimmed.Contains("__")
               || Regex.IsMatch(
                   trimmed,
                   @"\[[^\]]+\]\([^)]+\)"
               );
    }

    public override Task PopulateMetadataAsync(string text)
    {
        return Task.CompletedTask;
    }
}