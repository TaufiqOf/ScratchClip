using ScratchClip.Views;

namespace ScratchClip.Helper;

public static class WindowStateManager
{
    public static MainWindow MainWindow { get; private set; }
    
    public static void Initialize(MainWindow mainWindow)
    {
        MainWindow = mainWindow;
    }
}