using System.IO;
using System.Linq;

namespace MelonLoader.Installer.Core;

/// <summary>
/// Class for storing information only used internally
/// </summary>
internal class PatchInfo
{
    public string LemonDataDirectory { get; } = "";

    public string UnityBaseDirectory { get; } = "";
    public string UnityNativeDirectory { get; } = "";
    public string UnityManagedDirectory { get; } = "";

    public string OutputBaseApkPath { get; } = "";
    public string? OutputLibApkPath { get; } = null;
    public string[]? OutputExtraApkPaths { get; } = null;

    public string PemData { get; set; } = "";

    private readonly PatchArguments _arguments;

    public PatchInfo(PatchArguments arguments)
    {
        _arguments = arguments;

        LemonDataDirectory = Path.Combine(arguments.TempDirectory, "lemon_data");

        UnityBaseDirectory = Path.Combine(arguments.TempDirectory, "unity");
        UnityNativeDirectory = Path.Combine(UnityBaseDirectory, "Libs");
        UnityManagedDirectory = Path.Combine(UnityBaseDirectory, "Managed");

        OutputBaseApkPath = Path.Combine(arguments.OutputApkDirectory, "base.apk");
        if (_arguments.IsSplit)
            OutputLibApkPath = Path.Combine(arguments.OutputApkDirectory, Path.GetFileName(arguments.LibraryApkPath));
        if (_arguments.ExtraSplitApkPaths != null)
            OutputExtraApkPaths = _arguments.ExtraSplitApkPaths.Select(p => Path.Combine(arguments.OutputApkDirectory, Path.GetFileName(p))).ToArray();
    }

    public void CreateDirectories()
    {
        Directory.CreateDirectory(_arguments.TempDirectory);
        Directory.CreateDirectory(LemonDataDirectory);
    }
}
