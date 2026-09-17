using CommunityToolkit.Mvvm.ComponentModel;

namespace ScratchClip.ViewModels;

public partial class MenuWindowViewModel: ViewModelBase
{
    
    [ObservableProperty]
    private string _searchText;
    
}