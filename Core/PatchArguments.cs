namespace MelonLoader.Installer.Core;

/// <summary>
/// Public-facing class for user set information
/// </summary>
public class PatchArguments
{
    public string TargetApkPath { get; internal set; } = "";
    public string LibraryApkPath { get; internal set; } = "";
    public string[] ExtraSplitApkPaths { get; internal set; } = [];

    public string OutputApkDirectory { get; internal set; } = "";

    public string TempDirectory { get; internal set; } = "";

    public string MelonDataPath { get; internal set; } = "";
    public string UnityDependenciesPath { get; internal set; } = "";

    public AssetRipper.Primitives.UnityVersion? UnityVersion { get; internal set; }
    public string PackageName { get; internal set; } = "";

    public bool IsSplit { get; internal set; }

    public PatchArguments(string targetApkPath, string libraryApkPath, string[] extraSplitApkPaths, string outputApkDirectory, string tempDirectory, string melonDataPath, string unityDependenciesPath, AssetRipper.Primitives.UnityVersion? unityVersion, string packageName, bool isSplit)
    {
        TargetApkPath = targetApkPath;
        LibraryApkPath = libraryApkPath;
        ExtraSplitApkPaths = extraSplitApkPaths;
        OutputApkDirectory = outputApkDirectory;
        TempDirectory = tempDirectory;
        MelonDataPath = melonDataPath;
        UnityDependenciesPath = unityDependenciesPath;
        UnityVersion = unityVersion;
        PackageName = packageName;
        IsSplit = isSplit;
    }
}