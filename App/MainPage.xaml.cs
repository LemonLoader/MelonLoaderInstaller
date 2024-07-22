using MelonLoader.Installer.App.Utils;

namespace MelonLoader.Installer.App;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();

        AndroidPermissionHandler.Check();
    }
}