using MelonLoader.Installer.App.Utils;

namespace MelonLoader.Installer.App;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();

#if ANDROID
        ADBDevicesTab.IsVisible = false;
		// can't change tab here as the android context isn't fully setup
#else
		PermissionsTab.IsVisible = false;
		CurrentItem = ADBDevicesTab;
#endif
	}

    protected override async void OnAppearing()
    {
		await PackageWarningManager.Retrieve();

        base.OnAppearing();
    }
}
