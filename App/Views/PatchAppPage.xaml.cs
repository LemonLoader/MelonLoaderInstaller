using MelonLoader.Installer.App.ViewModels;

namespace MelonLoader.Installer.App.Views;

public partial class PatchAppPage : ContentPage
{
	public PatchAppPage()
	{
		InitializeComponent();
        BindingContext = new PatchAppPageViewModel();
        // TODO: toggle RestoreAPKButton dependnding on if an unpatched apk exists
    }

    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        AppDisplay.BindingContext = PatchAppPageViewModel.CurrentAppData;

        base.OnNavigatedTo(args);
    }
}