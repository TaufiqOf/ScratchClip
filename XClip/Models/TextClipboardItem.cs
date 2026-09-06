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
        _ => TextClipboardItemType.PlainText
    };

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

    private static ATextType CreateTextType(string text)
    {
        var candidates = new ATextType[]
        {
            new WebsiteTextType(text),
            new JsonTextType(text),
            new XmlTextType(text),
            new CodeTextType(text),
            new MarkdownTextType(text),
            new PlainTextType(text)
        };

        return candidates.First(type => type.IsMatch(text));
    }

    private void UpdateTags()
    {
        var tags = new List<string> { "Text" };

        switch (Type)
        {
            case TextClipboardItemType.Website:
                tags.Add("Website");
                break;
            case TextClipboardItemType.Code:
                tags.Add("Code");
                break;
            case TextClipboardItemType.Xml:
                tags.Add("Xml");
                break;
            case TextClipboardItemType.Json:
                tags.Add("Json");
                break;
            case TextClipboardItemType.Markdown:
                tags.Add("Markdown");
                break;
        }

        Tags.Clear();
        foreach (var tag in tags.Distinct())
            Tags.Add(tag);
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