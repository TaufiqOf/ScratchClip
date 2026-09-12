namespace ScratchClip.Models;

public enum TextClipboardItemType
{
    [System.ComponentModel.Description("PLAINTEXT")]
    PlainText,
    [System.ComponentModel.Description("WEBSITE")]
    Website,
    [System.ComponentModel.Description("CODE")]
    Code,
    [System.ComponentModel.Description("XML")]
    Xml,
    [System.ComponentModel.Description("JSON")]
    Json,
    [System.ComponentModel.Description("MARKDOWN")]
    Markdown,
    [System.ComponentModel.Description("PASSWORD")]
    Password
}