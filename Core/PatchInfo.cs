using System.IO;
using System.Linq;

namespace MelonLoader.Installer.Core;

/// <summary>
/// Class for storing patch information
/// </summary>
public class PatchInfo
{
    public string LemonDataDirectory { get; } = "";

    public string UnityNativeDirectory { get; set; } = "";

    public string OutputBaseApkPath { get; } = "";
    public string? OutputLibApkPath { get; } = null;
    public string[]? OutputExtraApkPaths { get; } = null;

    public string PemData { get; set; } = "";

    private readonly PatchArguments _arguments;

    internal PatchInfo(PatchArguments arguments)
    {
        _arguments = arguments;

        LemonDataDirectory = Path.Combine(arguments.TempDirectory, "lemon_data");

        UnityNativeDirectory = Path.Combine(arguments.TempDirectory, "unity");

        OutputBaseApkPath = Path.Combine(arguments.OutputApkDirectory, "base.apk");
        if (_arguments.IsSplit)
            OutputLibApkPath = Path.Combine(arguments.OutputApkDirectory, Path.GetFileName(arguments.LibraryApkPath));
        if (_arguments.ExtraSplitApkPaths != null)
            OutputExtraApkPaths = _arguments.ExtraSplitApkPaths.Select(p => Path.Combine(arguments.OutputApkDirectory, Path.GetFileName(p))).ToArray();
    }

    internal void CreateDirectories()
    {
        Directory.CreateDirectory(_arguments.TempDirectory);
        Directory.CreateDirectory(LemonDataDirectory);
    }
}
