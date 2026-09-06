using System.Threading.Tasks;
using XClip.ViewModels;

namespace XClip.Models;

public abstract class ATextType : ViewModelBase
{
    public abstract string DisplayName { get; }
    public abstract bool IsMatch(string text);
    public abstract Task PopulateMetadataAsync(string text);
}