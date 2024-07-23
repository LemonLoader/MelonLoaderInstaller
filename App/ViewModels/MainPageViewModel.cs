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

    private CancellationTokenSource? _appSearchTokenSource = null;
    private Thread? _appSearchThread = null;

    public MainPageViewModel()
    {
        ItemTappedCommand = new Command<UnityApplicationFinder.Data>(OnItemTapped);
        OnAppAddingComplete = null;

        ADBManager.OnPrimaryDeviceChanged += StartNewAppSearchThread;
        StartNewAppSearchThread();
    }

    private void StartNewAppSearchThread()
    {
        _appSearchTokenSource?.Cancel();
        _appSearchThread?.Join();

        _appSearchTokenSource = new();
        _appSearchThread = new(() => AddAllApps(_appSearchTokenSource.Token));
        _appSearchThread.Start();
    }

    private void AddAllApps(CancellationToken token = default)
    {
        Items.Clear();
        foreach (var app in UnityApplicationFinder.Find(token))
        {
            if (token.IsCancellationRequested)
            {
                Items.Clear();
                return;
            }

            Items.AddOnUI(app);
        }

        if (token.IsCancellationRequested)
        {
            Items.Clear();
            return;
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