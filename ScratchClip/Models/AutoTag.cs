namespace ScratchClip.Models;

public class AutoTag
{
    private string _tagName = string.Empty;

    public string TagName
    {
        get => _tagName;
        set => _tagName = value?.ToUpperInvariant() ?? string.Empty;
    }

    public string Regex { get; set; } = string.Empty;
}