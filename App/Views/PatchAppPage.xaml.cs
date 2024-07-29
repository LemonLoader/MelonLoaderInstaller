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

#if ANDROID
        if (PatchAppPageViewModel.CurrentAppData.Source == UnityApplicationFinder.Source.PackageManager)
        {
            string packageName = PatchAppPageViewModel.CurrentAppData.PackageName;

            string basePath = Android.OS.Environment.ExternalStorageDirectory!.AbsolutePath;
            string dataPath = Path.Combine(basePath, "Android", "data", packageName);
            string obbPath = Path.Combine(basePath, "Android", "obb", packageName);

            bool hasObbPath = Directory.Exists(obbPath);
            bool hasObbFiles = hasObbPath && Directory.GetFiles(obbPath, "*.obb").Length != 0;

            bool hasObb = hasObbPath && hasObbFiles;

            if (hasObb)
            {
                await PopupHelper.Alert("This app has OBB data which will get deleted during the patching process. This installer does not back up these files, which could result in a non-functional application. Please back up these files before continuing.", "Warning");
            }
        }
#endif

        if (PackageWarningManager.AvailableWarnings.TryGetValue(PatchAppPageViewModel.CurrentAppData.PackageName, out string? warning) && warning != null)
            await PopupHelper.Alert(warning, "Warning");

        string? backupDir = PatchAppPageViewModel.CurrentAppData.GetBackupDirectory();
        if (backupDir != null && Directory.Exists(backupDir) && Directory.GetFiles(backupDir, "*.apk").Length > 0)
            RestoreAPKButton.IsVisible = true;
        else
            RestoreAPKButton.IsVisible = false;

        base.OnNavigatedTo(args);
    }
}