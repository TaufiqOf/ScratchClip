using System.Collections.ObjectModel;
using System.Threading.Tasks;
using FluentIcons.Common;
using ScratchClip.ViewModels;

namespace ScratchClip.Models;

public abstract class ATextType : ViewModelBase
{
    private ObservableCollection<string> _tags;

    public ATextType(string text, ObservableCollection<string> tags)
    {
        Text = text;
        _tags = tags;
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

    public ObservableCollection<string> Tags => _tags;
    private Icon _icon;
    public Icon Icon
    {
        get => _icon;
        set
        {
            _icon = value;
            OnPropertyChanged();
        }
    }
    public abstract string DisplayName { get; }
    public abstract bool IsMatch(string text);
    public abstract Task PopulateMetadataAsync(string text);
    

}