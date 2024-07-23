using CommunityToolkit.Maui.Alerts;
using MelonLoader.Installer.App.Utils;
using MelonLoader.Installer.App.Views;
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
        // TODO: hook into adbmanager to know when a new device is selected so getting apps can be redone
        ItemTappedCommand = new Command<UnityApplicationFinder.Data>(OnItemTapped);
        OnAppAddingComplete = null;

        new Thread(AddAllApps).Start();
    }

    private void AddAllApps()
    {
        Items.Clear();
        foreach (var app in UnityApplicationFinder.Find())
        {
            Items.AddOnUI(app);
        }

        Application.Current!.Dispatcher.Dispatch(() => OnAppAddingComplete?.Invoke());
    }

    private async void OnItemTapped(UnityApplicationFinder.Data item)
    {
        System.Diagnostics.Debug.WriteLine(item.AppName);

        if (item.Status == UnityApplicationFinder.Status.Unsupported)
        {
            var toast = Toast.Make("This app is unsupported.", CommunityToolkit.Maui.Core.ToastDuration.Long);
            await toast.Show();
            return;
        }

        if (!AndroidPermissionHandler.HaveRequired())
        {
            var toast = Toast.Make("Permissions are not setup.", CommunityToolkit.Maui.Core.ToastDuration.Long);
            await toast.Show();

            await Shell.Current.GoToAsync(nameof(PermissionSetupPage));
            return;
        }

        // TODO: app view page
    }
}