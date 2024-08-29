using CommunityToolkit.Maui.Core;

namespace MelonLoader.Installer.App.Utils;

public static class PopupHelper
{
    /// <summary>
    /// Shows a pop up with text. Best for showing errors.
    /// Uses an alert on Windows because it looks better and makes more sense.
    /// </summary>
    /// <param name="message">Description</param>
    /// <param name="title">Title of alert; only applicable to Windows</param>
    /// <param name="duration">Duration of alert; only applicable to Android</param>
    public static async Task Toast(string message, ToastDuration duration = ToastDuration.Long, string title = "")
    {
#if ANDROID
        var toast = CommunityToolkit.Maui.Alerts.Toast.Make(message, duration);
        await toast.Show();
#else
        await Application.Current!.MainPage!.DisplayAlert(title, message, "Ok");
#endif
    }

    public static async Task Alert(string message, string title, string cancel = "Ok")
    {
        await Application.Current!.MainPage!.DisplayAlert(title, message, cancel);
    }
}
