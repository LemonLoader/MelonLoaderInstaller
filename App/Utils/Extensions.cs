namespace MelonLoader.Installer.App.Utils;

internal static class Extensions
{
    public static void AddOnUI<T>(this ICollection<T> collection, T item)
    {
        Application.Current!.Dispatcher.Dispatch(() => collection.Add(item));
    }
}
