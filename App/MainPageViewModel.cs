using MelonLoader.Installer.App.Utils;
using System.Collections.ObjectModel;

namespace MelonLoader.Installer.App;
public class MainPageViewModel : BindableObject
{
    public ObservableCollection<UnityApplicationFinder.Data> Items { get; protected set; } = [];

    public MainPageViewModel()
    {
        // todo: put this on a separate thread to load the ui asap
        var apps = UnityApplicationFinder.Find();
        foreach (var app in apps)
        {
            Items.Add(app);
        }
    }
}