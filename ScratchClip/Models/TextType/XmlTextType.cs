using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Xml.Linq;
using FluentIcons.Common;

namespace ScratchClip.Models.TextType;

public class XmlTextType : ATextType
{
    public XmlTextType(string text, ObservableCollection<string> tags) : base(text, tags)
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