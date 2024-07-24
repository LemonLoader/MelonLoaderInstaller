namespace MelonLoader.Installer.App.Utils;

internal static class Extensions
{
    public static void ClearOnUI<T>(this ICollection<T> collection)
    {
        Application.Current!.Dispatcher.Dispatch(() => collection.Clear());
    }

    public static void AddOnUI<T>(this ICollection<T> collection, T item)
    {
        Application.Current!.Dispatcher.Dispatch(() => collection.Add(item));
    }

    public static void GoToTabOnFirst(this Shell self, string name)
    {
        GoToTab(self, self.Items.First(), name);
    }

    public static void GoToTab(this Shell self, ShellItem item, string name)
    {
        var tab = item.Items.First(j => j is Tab tab && tab.Route == name);
        self.CurrentItem = tab;
    }
}
