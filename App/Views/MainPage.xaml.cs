using MelonLoader.Installer.App.Utils;

namespace MelonLoader.Installer.App.Views;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();

        AndroidPermissionHandler.Check();
    }
}