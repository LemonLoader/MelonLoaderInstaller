using CommunityToolkit.Maui.Alerts;
using MelonLoader.Installer.App.Utils;
using MelonLoader.Installer.App.Views;
using System.Collections.ObjectModel;
using System.IO.Compression;
using System.Windows.Input;

namespace MelonLoader.Installer.App.ViewModels;

public class MainPageViewModel : BindableObject
{
    public ObservableCollection<UnityApplicationFinder.Data> Items { get; protected set; } = [];

    public ICommand ItemTappedCommand { get; }
    public ICommand SelectAPKButtonCommand { get; }

    public Action? OnAppAddingComplete { get; set; }
    public Action? OnAppAddingReset { get; set; }

    private CancellationTokenSource? _appSearchTokenSource = null;
    private Thread? _appSearchThread = null;

    public MainPageViewModel()
    {
        ItemTappedCommand = new Command<UnityApplicationFinder.Data>(OnItemTapped);
        SelectAPKButtonCommand = new Command(OnTapSelectAPKButton);
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

    private async void OnTapSelectAPKButton()
    {
        FilePickerFileType apkType = new(new Dictionary<DevicePlatform, IEnumerable<string>>
        {
            { DevicePlatform.Android, [ "application/vnd.android.package-archive" ] },
            { DevicePlatform.WinUI, [ ".apk" ] }
        });

        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions() { FileTypes = apkType });
            if (result != null)
            {
                try
                {
                    using FileStream apkStream = new(result.FullPath, FileMode.Open);
                    using ZipArchive archive = new(apkStream, ZipArchiveMode.Read);

                    bool hasArm64Dir = archive.Entries.Any(a => a.FullName.Contains("arm64-v8a"));

                    if (!hasArm64Dir)
                    {
                        var toast = Toast.Make("Selected APK does not support ARM64 and cannot be patched.", CommunityToolkit.Maui.Core.ToastDuration.Long);
                        await toast.Show();
                        return;
                    }

                    var unityLib = archive.GetEntry("lib/arm64-v8a/libunity.so");
                    var il2cppLib = archive.GetEntry("lib/arm64-v8a/libil2cpp.so");

                    if (unityLib == null)
                    {
                        var toast = Toast.Make("Selected APK is not a Unity app and cannot be patched.", CommunityToolkit.Maui.Core.ToastDuration.Long);
                        await toast.Show();
                        return;
                    }

                    if (il2cppLib == null)
                    {
                        var toast = Toast.Make("Selected APK is not IL2CPP and cannot be patched.", CommunityToolkit.Maui.Core.ToastDuration.Long);
                        await toast.Show();
                        return;
                    }
                }
                catch (Exception ex)
                {
                    var toast = Toast.Make("Selected APK is invalid or unsupported.", CommunityToolkit.Maui.Core.ToastDuration.Long);
                    await toast.Show();

                    System.Diagnostics.Debug.WriteLine(ex);

                    return;
                }

                UnityApplicationFinder.Data data = new(Path.GetFileNameWithoutExtension(result.FullPath), "", UnityApplicationFinder.Status.Unpatched, UnityApplicationFinder.Source.File, [result.FullPath], null);
                PatchAppPageViewModel.CurrentAppData = data;
                Shell.Current.GoToTabOnFirst(nameof(PatchAppPage));
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
        }
    }

    private async void OnItemTapped(UnityApplicationFinder.Data item)
    {
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

        PatchAppPageViewModel.CurrentAppData = item;
        Shell.Current.GoToTabOnFirst(nameof(PatchAppPage));
    }
}