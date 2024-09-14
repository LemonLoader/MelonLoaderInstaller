using AdvancedSharpAdbClient.Models;
using MelonLoader.Installer.App.Utils;
using MelonLoader.Installer.App.Views;
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
            await PopupHelper.Toast("Selected device is unavailable. Make sure you gave permissions on your device.");
            return;
        }

        if (!ADBManager.IsArm64(item))
        {
            await PopupHelper.Toast("Selected device does not support ARM64, which is required for the loader to function.");
            return;
        }

        ADBManager.SetPrimaryDevice(item);
        Shell.Current.GoToTabOnFirst(nameof(MainPage));
    }

    private void RefreshDevices()
    {
        Devices.Clear();
        var devices = ADBManager.GetDevices();
        foreach (var device in devices)
            Devices.Add(device);
    }

    private void GoBackToMain()
    {
        Shell.Current.GoToTabOnFirst(nameof(MainPage));
    }
}
