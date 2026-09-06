using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace XClip.Models.TextType;

public partial class PlainTextType : ATextType
{
    [ObservableProperty]
    private string _text;
    public override string DisplayName => "Plain Text";

    public override bool IsMatch(string text)
    {
        return true;
    }

    public override Task PopulateMetadataAsync(string text)
    {
        Text = text;
        return Task.CompletedTask;
    }
}