using MelonLoader.Installer.App.Utils;
using MelonLoader.Installer.App.ViewModels;

namespace MelonLoader.Installer.App.Views;

public partial class PatchAppPage : ContentPage
{
	public PatchAppPage()
	{
		InitializeComponent();
        BindingContext = new PatchAppPageViewModel();
    }

    protected override async void OnNavigatedTo(NavigatedToEventArgs args)
    {
        AppDisplay.BindingContext = PatchAppPageViewModel.CurrentAppData;

        string? backupDir = PatchAppPageViewModel.CurrentAppData.GetBackupDirectory();
        if (backupDir != null && Directory.Exists(backupDir) && Directory.GetFiles(backupDir, "*.apk").Length > 0)
            RestoreAPKButton.IsVisible = true;
        else
            RestoreAPKButton.IsVisible = false;

#if ANDROID
        if (PatchAppPageViewModel.CurrentAppData.Source == UnityApplicationFinder.Source.PackageManager)
            await PopupHelper.Alert("This app may have data which will get deleted during the patching process. This installer does not back up these files, which could result in a non-functional application. Please back up these files before continuing.", "Warning");
#endif

        if (PackageWarningManager.AvailableWarnings.TryGetValue(PatchAppPageViewModel.CurrentAppData.PackageName, out string? warning) && warning != null)
            await PopupHelper.Alert(warning, "Warning");

        base.OnNavigatedTo(args);
    }
}