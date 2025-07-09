namespace MelonLoader.Installer.Core;

/// <summary>
/// Public-facing class for user set information
/// </summary>
public class PatchArguments
{
    public string TargetApkPath { get; set; } = "";
    public string LibraryApkPath { get; set; } = "";
    public string[] ExtraSplitApkPaths { get; set; } = [];

    public string OutputApkDirectory { get; set; } = "";

    public string TempDirectory { get; set; } = "";

    public string MelonDataPath { get; set; } = "";
    public string UnityDependenciesPath { get; set; } = "";

    public AssetRipper.Primitives.UnityVersion? UnityVersion { get; set; }
    public string PackageName { get; set; } = "";

    public bool IsSplit { get; set; }
}