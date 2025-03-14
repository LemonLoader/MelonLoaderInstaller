using MelonLoader.Installer.App.Utils;
using MelonLoader.Installer.App.ViewModels;

namespace MelonLoader.Installer.App.Views;

public partial class PatchAppPage : ContentPage
{
    private string? _lastWarnedPackage;

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

        bool warnThisTime = _lastWarnedPackage != PatchAppPageViewModel.CurrentAppData.PackageName;

#if ANDROID
        if (warnThisTime && PatchAppPageViewModel.CurrentAppData.Source == UnityApplicationFinder.Source.PackageManager)
            await PopupHelper.Alert("This app may have data which will get deleted during the patching process. This installer does not back up these files, which could result in a non-functional application. Please back up these files before continuing.", "Warning");
#endif

        if (warnThisTime && PackageWarningManager.AvailableWarnings.TryGetValue(PatchAppPageViewModel.CurrentAppData.PackageName, out string? warning) && warning != null)
            await PopupHelper.Alert(warning, "Warning");

        _lastWarnedPackage = PatchAppPageViewModel.CurrentAppData.PackageName;

        base.OnNavigatedTo(args);
    }
}