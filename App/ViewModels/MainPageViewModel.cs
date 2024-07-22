using MelonLoader.Installer.App.Utils;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace MelonLoader.Installer.App.ViewModels;

public class MainPageViewModel : BindableObject
{
    public ObservableCollection<UnityApplicationFinder.Data> Items { get; protected set; } = [];

    public ICommand ItemTappedCommand { get; }

    public Action? OnAppAddingComplete { get; set; }

    public MainPageViewModel()
    {
        ItemTappedCommand = new Command<UnityApplicationFinder.Data>(OnItemTapped);
        OnAppAddingComplete = null;

        new Thread(AddAllApps).Start();
    }

    private void AddAllApps()
    {
        foreach (var app in UnityApplicationFinder.Find())
        {
            Items.AddOnUI(app);
        }

        Application.Current!.Dispatcher.Dispatch(() => OnAppAddingComplete?.Invoke());
    }

    private void OnItemTapped(UnityApplicationFinder.Data item)
    {
        System.Diagnostics.Debug.WriteLine(item.AppName);
    }
}