using CommunityToolkit.Maui.Alerts;
using MelonLoader.Installer.App.Utils;
using System.Windows.Input;

namespace MelonLoader.Installer.App.ViewModels;

public class PatchAppPageViewModel : BindableObject
{
    public static UnityApplicationFinder.Data CurrentAppData { get => _currentAppData ?? _dummyAppData; set => _currentAppData = value; }

    private static UnityApplicationFinder.Data _dummyAppData = new("No app selected.", "Please choose one from the apps tab.", UnityApplicationFinder.Status.Unpatched, UnityApplicationFinder.Source.None, [], null);
    private static UnityApplicationFinder.Data? _currentAppData;

    public ICommand PatchTappedCommand { get; }

    public PatchAppPageViewModel()
    {
        PatchTappedCommand = new Command(DoPatch);
    }

    private async void DoPatch()
    {
        if (_currentAppData == null)
        {
            var toast = Toast.Make("No app selected.", CommunityToolkit.Maui.Core.ToastDuration.Short);
            await toast.Show();

            return;
        }
    }
}