using AdvancedSharpAdbClient.Models;
using CommunityToolkit.Maui.Alerts;
using MelonLoader.Installer.App.Utils;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace MelonLoader.Installer.App.ViewModels;

public class SelectADBDevicePageViewModel : BindableObject
{
    public ObservableCollection<DeviceData> Devices { get; protected set; } = [];

    public ICommand DeviceTappedCommand { get; }
    public ICommand RefreshButtonCommand { get; }
    public ICommand ContinueTappedCommand { get; }

    public SelectADBDevicePageViewModel()
    {
        DeviceTappedCommand = new Command<DeviceData>(OnDeviceTapped);
        RefreshButtonCommand = new Command(RefreshDevices);
        ContinueTappedCommand = new Command(GoBackToMain);

        RefreshDevices();
    }

    private async void OnDeviceTapped(DeviceData item)
    {
        if (item.State is DeviceState.NoPermissions or DeviceState.Unknown or DeviceState.Offline or DeviceState.Unauthorized)
        {
            var toast = Toast.Make("Selected device is unavailable. Make sure you gave permissions on your device.", CommunityToolkit.Maui.Core.ToastDuration.Long);
            await toast.Show();
            return;
        }

        if (!ADBManager.IsArm64(item))
        {
            var toast = Toast.Make("Selected device does not support ARM64, which is required for the loader to function.", CommunityToolkit.Maui.Core.ToastDuration.Long);
            await toast.Show();
            return;
        }

        ADBManager.SetPrimaryDevice(item);
        await Shell.Current.GoToAsync("..");
    }

    private void RefreshDevices()
    {
        Devices.Clear();
        var devices = ADBManager.GetDevices();
        foreach (var device in devices)
            Devices.Add(device);
    }

    private async void GoBackToMain()
    {
        await Shell.Current.GoToAsync("..");
    }
}
