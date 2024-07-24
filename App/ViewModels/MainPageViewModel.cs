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
    public Action? OnAppAddingReset { get; set; }

    private CancellationTokenSource? _appSearchTokenSource = null;
    private Thread? _appSearchThread = null;

    public MainPageViewModel()
    {
        ItemTappedCommand = new Command<UnityApplicationFinder.Data>(OnItemTapped);
        OnAppAddingComplete = null;
        OnAppAddingReset = null;

        try
        {
            ADBManager.OnPrimaryDeviceChanged += StartNewAppSearchThread;
        }
        catch { } // this throws a null-ref on Android for some reason

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
        Application.Current!.Dispatcher.Dispatch(() => OnAppAddingReset?.Invoke());

        Items.ClearOnUI();
        foreach (var app in UnityApplicationFinder.Find(token))
        {
            if (token.IsCancellationRequested)
            {
                Items.ClearOnUI();
                return;
            }

            Items.AddOnUI(app);
        }

        if (token.IsCancellationRequested)
        {
            Items.ClearOnUI();
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

            Shell.Current.GoToTabOnFirst(nameof(PermissionSetupPage));
            return;
        }

        // TODO: app view page
    }
}