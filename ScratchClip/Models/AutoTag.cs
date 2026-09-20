using ScratchClip.ViewModels;

namespace ScratchClip.Models;

public class AutoTag: ViewModelBase
{
    private string _tagName = string.Empty;

    public string TagName
    {
        get => _tagName;
        set => SetProperty(ref _tagName, value?.ToUpperInvariant() ?? string.Empty);
    }

    public string Regex
    {
        get;
        set => SetProperty(ref field, value);
    } = string.Empty;

    public bool Enabled
    {
        get;
        set => SetProperty(ref field, value);
    } = true;
}