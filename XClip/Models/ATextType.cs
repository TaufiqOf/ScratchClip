using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using FluentIcons.Common;
using XClip.ViewModels;

namespace XClip.Models;

public abstract class ATextType : ViewModelBase
{
    public ATextType(string text)
    {
        Text = text;
    }
    private string _text = string.Empty;
    public string Text
    {
        get => _text;
        set
        {
            _text = value;
            OnPropertyChanged();
        }
    }
    private Icon _icon;
    public Icon Icon
    {
        get => _icon;
        set
        {
            _icon = value;
            OnPropertyChanged();;
        }
    }
    public abstract string DisplayName { get; }
    public abstract bool IsMatch(string text);
    public abstract Task PopulateMetadataAsync(string text);
    

}