using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using XClip.Manager;
using XClip.Models.TextType;

namespace XClip.Models;

public partial class TextClipboardItem : AClipboardItem
{
    private ATextType? _textType;

    public TextClipboardItem()
    {
        Tags.Add("Text");
    }

    public ATextType? TextType
    {
        get => _textType;
        private set
        {
            if (ReferenceEquals(_textType, value)) return;
            _textType = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Type));
        }
    }

    public TextClipboardItemType Type => TextType switch
    {
        WebsiteTextType => TextClipboardItemType.Website,
        CodeTextType => TextClipboardItemType.Code,
        XmlTextType => TextClipboardItemType.Xml,
        JsonTextType => TextClipboardItemType.Json,
        MarkdownTextType => TextClipboardItemType.Markdown,
        PasswordTextType => TextClipboardItemType.Password,
        _ => TextClipboardItemType.PlainText
    };

    public bool IsPassword => Type == TextClipboardItemType.Password;

    [RelayCommand]
    private void ItemClicked()
    {
        
    }

    public async Task PopulateMetadataAsync()
    {
        var text = Text;

        TextType = CreateTextType(text);
        UpdateTags();

        await TextType.PopulateMetadataAsync(text);
    }



    private ATextType CreateTextType(string text)
    {
        var candidates = new ATextType[]
        {
            new WebsiteTextType(text,Tags),
            new JsonTextType(text,Tags),
            new XmlTextType(text,Tags),
            new CodeTextType(text,Tags),
            new MarkdownTextType(text,Tags),
            new PasswordTextType(text,Tags),
            new PlainTextType(text,Tags),//must be last, as it will match anything
        };

        return candidates.First(type => type.IsMatch(text));
    }

    private void UpdateTags()
    {
        // var tags = new List<string> { "Text" };
        Tags.Add(Type.ToString());
        // foreach (var tag in tags.Distinct())
        //     Tags.Add(tag);
    }
    [RelayCommand]
    private async Task CopyToClipboardAsync()
    {
        await ClipboardManager.SetClipboardItemAsync(this);
    }

    [RelayCommand]
    private void Delete()
    {
        OnDelete?.Invoke(this);
    }
}