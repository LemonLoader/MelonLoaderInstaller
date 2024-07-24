using MelonLoader.Installer.App.Utils;

namespace MelonLoader.Installer.App.ViewModels;

public class PatchAppPageViewModel : BindableObject
{
    public static UnityApplicationFinder.Data CurrentAppData { get => currentAppData ?? _dummyAppData; set => currentAppData = value; }

    private static UnityApplicationFinder.Data _dummyAppData = new("No app selected.", "Please choose one from the apps tab.", UnityApplicationFinder.Status.Unpatched, [], null);
    private static UnityApplicationFinder.Data? currentAppData;

    public PatchAppPageViewModel()
    {
    }
}