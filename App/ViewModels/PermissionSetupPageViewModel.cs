using MelonLoader.Installer.App.Utils;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace MelonLoader.Installer.App.ViewModels;

public class PermissionSetupPageViewModel : BindableObject
{
    public ObservableCollection<RequiredPermission> Permissions { get; protected set; } = [];

    public ICommand ItemTappedCommand { get; }
    public ICommand ContinueTappedCommand { get; }

    public PermissionSetupPageViewModel()
    {
        ItemTappedCommand = new Command<RequiredPermission>(OnItemTapped);
        ContinueTappedCommand = new Command(OnContinueTapped);

        /*Permissions.Add(new(AndroidPermissionHandler.HasExternalStorage)
        {
            Name = "External Storage Permissions",
            Description = "Required to access storage data during patching.",
            Request = AndroidPermissionHandler.TryGetExternalStorage,
        });*/

        Permissions.Add(new(AndroidPermissionHandler.HasAccessToAllFiles)
        {
            Name = "Manage All Files Permission",
            Description = "Required to access storage data during patching.",
            Request = AndroidPermissionHandler.TryGetAccessToAllFiles,
        });

        Permissions.Add(new(AndroidPermissionHandler.CanInstallUnknownSources)
        {
            Name = "Allow Unknown Sources",
            Description = "Required to install the patched game.",
            Request = AndroidPermissionHandler.TryGetInstallUnknownSources,
        });
    }

    private void OnItemTapped(RequiredPermission item)
    {
        item.Request?.Invoke();
    }

    private async void OnContinueTapped()
    {
        await Shell.Current.GoToAsync("..");
    }

    public class RequiredPermission
    {
        public string Name { get; set; }
        public string Description { get; set; }

        public Action Request { get; set; }
        public Func<bool>? DoneCheck { get; set; }

        #region UI Bindings

        private bool _doneCheck = false;
        public Color ButtonBackgroundColor => _doneCheck == true ? (Color)Application.Current!.Resources["Dark"] : (Color)Application.Current!.Resources["Light"];
        public string ButtonText => _doneCheck == true ? "Already Setup" : "Press to Setup";

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public RequiredPermission(Func<bool>? doneCheck = null)
        {
            DoneCheck = doneCheck;
            _doneCheck = DoneCheck?.Invoke() ?? false;
        }
#pragma warning restore CS8618

        #endregion
    }
}