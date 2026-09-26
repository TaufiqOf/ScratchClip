using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace ScratchClip.Models.TextType;

public partial class YoutubeLikeTextType : ATextType
{
    public YoutubeLikeTextType(string text, ObservableCollection<string> tags) : base(text, tags)
    {
    }

    public override string DisplayName { get; }
    public override string SuggestedExtension { get; }
    public override bool IsMatch(string text)
    {
        throw new System.NotImplementedException();
    }

    public override Task PopulateMetadataAsync(string text)
    {
        throw new System.NotImplementedException();
    }

    public override Task UpdateTagsAsync(string text)
    {
        throw new System.NotImplementedException();
    }
}