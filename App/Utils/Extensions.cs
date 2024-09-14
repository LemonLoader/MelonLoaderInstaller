using System.Text;

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

    public static bool IsBad(this string self)
    {
        byte[] stringBytes = Encoding.UTF8.GetBytes(self);
        byte[] hashBytes = System.Security.Cryptography.MD5.HashData(stringBytes);
        StringBuilder sb = new();
        for (int i = 0; i < hashBytes.Length; i++)
            sb.Append(hashBytes[i].ToString("x2"));
        string hash = sb.ToString();

        return _bad.Contains(hash);
    }


    private static readonly string[] _bad = [
        "95fb4cd16729627d013dc620a807c23c",
        "ffaf599e1b7e1175cd344b367e4a7ec4",
        "be1878f1900f48586eb7cab537f82f62",
        "196d46a42878aae4188839d35fdad747",
        "9b6f24bad02220abf7e12d7b4ad771f4",
        "a5595fbc343dbc2a468eb76533d345a5",
        "964c753427382e3bf56c1f7ee5a37f06",
        "e010d19cbf15c335d8f1852a1639c42c",
        "72cfa3439d21cc03ece7182cd494b75b",
        "0a4876540f4f7a11fd57a6ce54bbe0a7",
        "79aca3897e0c3e750a1f4b62776e8831",
        "f913df2dc82284a4689b8504bceb8241",
        "8239c5431b7656ab0e67ac78e6f807ff",
        "f810acd7cd40c97dfed703466476ceaa",
        "9396d377bfe52476013d5b007cfc19bf",
        "e56e5d1be5311620015cb070d11802ab",
        "6e8b0ebfaa80d548e3ee647281cbc627",
        "8e58816bb80589b154b77d84629f5a9e",
    ];
}
