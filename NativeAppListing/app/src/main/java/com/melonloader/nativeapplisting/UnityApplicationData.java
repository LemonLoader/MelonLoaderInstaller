package com.melonloader.nativeapplisting;

public class UnityApplicationData
{
    public String AppName;
    public String PackageName;
    public String[] ApkPaths;
    public Status Status;
    public String EncodedIcon;

    public UnityApplicationData(String appName, String packageName, String[] apkPaths, Status status, String encodedIcon)
    {
        AppName = appName;
        PackageName = packageName;
        ApkPaths = apkPaths;
        Status = status;
        EncodedIcon = encodedIcon;
    }

    public enum Status {
        Unpatched,
        Patched,
        Unsupported,
    }
}
