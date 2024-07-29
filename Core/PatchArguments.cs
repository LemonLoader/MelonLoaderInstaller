namespace MelonLoader.Installer.Core;

/// <summary>
/// Public-facing class for user set information
/// </summary>
public class PatchArguments
{
    public string TargetApkPath = "";
    public string LibraryApkPath = "";
    public string[] ExtraSplitApkPaths = [];

    public string OutputApkDirectory = "";

    public string TempDirectory = "";

    public string MelonDataPath = "";
    public string UnityDependenciesPath = "";

    public AssetRipper.Primitives.UnityVersion? UnityVersion;
    public string PackageName = "";

    public bool IsSplit;
}