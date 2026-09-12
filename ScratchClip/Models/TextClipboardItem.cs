using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Input;
using CommunityToolkit.Mvvm.Input;
using ScratchClip.Manager;
using ScratchClip.Models.TextType;

namespace ScratchClip.Models;

public partial class TextClipboardItem : AClipboardItem
{
    private ATextType? _textType;

    public TextClipboardItem()
    {
        Tags.Add("TEXT");
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
        if(TextType is PasswordTextType passwordTextType)
            DisplayText = new string('•', passwordTextType.Text.Length);
        await TextType.PopulateMetadataAsync(text);
    }



    private ATextType CreateTextType(string text)
    {
        var candidates = new ATextType[]
        {
            new WebsiteTextType(text,Tags,MataData),
            new JsonTextType(text,Tags,MataData),
            new XmlTextType(text,Tags,MataData),
            new CodeTextType(text,Tags,MataData),
            new MarkdownTextType(text,Tags,MataData),
            new PasswordTextType(text,Tags,MataData),
            new PlainTextType(text,Tags,MataData),//must be last, as it will match anything
        };

        return candidates.First(type => type.IsMatch(text));
    }
    

    private void UpdateTags()
    {
        // var tags = new List<string> { "Text" };
        Tags.Add(Type.ToString().ToUpperInvariant());
        // foreach (var tag in tags.Distinct())
        //     Tags.Add(tag);
    }
    [RelayCommand]
    private async Task CopyToClipboardAsync()
    {
        await ClipboardManager.SetClipboardItemAsync(this);
    }

    public override void Delete()
    {
        OnDelete?.Invoke(this);
    }

    public void UpdateByTags()
    {
        TextType = Tags.Contains("WEBSITE") ? new WebsiteTextType(Text, Tags, MataData) :
            Tags.Contains("JSON") ? new JsonTextType(Text, Tags, MataData) :
            Tags.Contains("XML") ? new XmlTextType(Text, Tags, MataData) :
            Tags.Contains("CODE") ? new CodeTextType(Text, Tags, MataData) :
            Tags.Contains("MARKDOWN") ? new MarkdownTextType(Text, Tags, MataData) :
            Tags.Contains("PASSWORD") ? new PasswordTextType(Text, Tags, MataData) :
            new PlainTextType(Text, Tags, MataData);
        if(TextType is PasswordTextType passwordTextType)
            DisplayText = new string('•', passwordTextType.Text.Length);
        else
        {
            DisplayText = TextType.Text.Length > 600 ? TextType.Text.Substring(0, 600) : TextType.Text;
        }
    }

    public void UpdateDisplayText()
    {
        
    }
}