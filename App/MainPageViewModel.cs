using MelonLoader.Installer.App.Utils;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace MelonLoader.Installer.App;
public class MainPageViewModel : BindableObject
{
    public ObservableCollection<UnityApplicationFinder.Data> Items { get; protected set; } = [];

    public ICommand ItemTappedCommand { get; }

    public MainPageViewModel()
    {
        ItemTappedCommand = new Command<UnityApplicationFinder.Data>(OnItemTapped);

        // todo: put this on a separate thread to load the ui asap
        var apps = UnityApplicationFinder.Find();
        foreach (var app in apps)
        {
            Items.Add(app);
        }
    }

    private void OnItemTapped(UnityApplicationFinder.Data item)
    {
        System.Diagnostics.Debug.WriteLine(item.AppName);
    }
}