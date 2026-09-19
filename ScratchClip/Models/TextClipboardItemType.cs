using System.ComponentModel;

namespace ScratchClip.Models;

public enum TextClipboardItemType
{
    [Description("PLAINTEXT")] PlainText,
    [Description("WEBSITE")] Website,
    [Description("CODE")] Code,
    [Description("XML")] Xml,
    [Description("JSON")] Json,
    [Description("MARKDOWN")] Markdown,
    [Description("PASSWORD")] Password
}