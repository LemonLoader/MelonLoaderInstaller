using CommunityToolkit.Maui.Alerts;
using MelonLoader.Installer.App.Utils;
using MelonLoader.Installer.App.Views;
using System.IO.Compression;
using System.Windows.Input;

namespace MelonLoader.Installer.App.ViewModels;

public class PatchAppPageViewModel : BindableObject
{
    public static UnityApplicationFinder.Data CurrentAppData { get => _currentAppData ?? _dummyAppData; set => _currentAppData = value; }

    private static UnityApplicationFinder.Data _dummyAppData = new("No app selected.", "Please choose one from the apps tab.", UnityApplicationFinder.Status.Unpatched, UnityApplicationFinder.Source.None, [], null);
    private static UnityApplicationFinder.Data? _currentAppData;

    public ICommand PatchTappedCommand { get; }
    public ICommand PatchLocalTappedCommand { get; }
    public ICommand RestoreTappedCommand { get; }

    public PatchAppPageViewModel()
    {
        PatchTappedCommand = new Command(DoPatch);
        PatchLocalTappedCommand = new Command(SelectLocalDepsWithPatch);
        RestoreTappedCommand = new Command(RestoreUnpatchedAPK);
    }

    private async void SelectLocalDepsWithPatch()
    {
        FilePickerFileType zipType = new(new Dictionary<DevicePlatform, IEnumerable<string>>
        {
            { DevicePlatform.Android, [ "application/zip" ] },
            { DevicePlatform.WinUI, [ ".zip" ] }
        });

        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions() { FileTypes = zipType });
            if (result != null)
            {
                try
                {
                    using FileStream apkStream = new(result.FullPath, FileMode.Open);
                    using ZipArchive archive = new(apkStream, ZipArchiveMode.Read);

                    bool hasArm64Dir = archive.Entries.Any(a => a.FullName.Contains("arm64-v8a"));

                    if (!hasArm64Dir)
                    {
                        await PopupHelper.Toast("Selected zip does not contain ARM64 libraries and cannot be used.");
                        return;
                    }

                    bool hasAnyUnityEngineDlls = archive.Entries.Any(a => a.FullName.StartsWith("Managed") && a.FullName.Contains("UnityEngine.") && a.FullName.EndsWith("dll"));

                    if (!hasAnyUnityEngineDlls)
                    {
                        await PopupHelper.Toast("Selected zip does not contain Unity DLLs and cannot be used.");
                        return;
                    }

                    var unityLib = archive.GetEntry("Libs/arm64-v8a/libunity.so");

                    if (unityLib == null)
                    {
                        await PopupHelper.Toast("Selected zip does not contain libunity.so and cannot be used.");
                        return;
                    }

                    // TODO: DoPatch with deps
                }
                catch (Exception ex)
                {
                    await PopupHelper.Toast("Selected zip is invalid.");

                    System.Diagnostics.Debug.WriteLine(ex);

                    return;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
        }
    }

    private async void DoPatch()
    {
        if (_currentAppData == null)
        {
            await PopupHelper.Toast("No app selected.", CommunityToolkit.Maui.Core.ToastDuration.Short);

            return;
        }
        
        // TODO: patch
    }

    private void RestoreUnpatchedAPK()
    {
        // TODO: restoring apk
    }
}