using AdvancedSharpAdbClient.Models;
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
