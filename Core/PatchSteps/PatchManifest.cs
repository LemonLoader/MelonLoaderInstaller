using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ionic.Zip;
using QuestPatcher.Axml;

namespace MelonLoader.Installer.Core.PatchSteps;

// based a lot on
// https://github.com/Lauriethefish/QuestPatcher/blob/main/QuestPatcher.Core/Patching/PatchingManager.cs
internal class PatchManifest : IPatchStep
{
    private static readonly Uri AndroidNamespaceUri = new("http://schemas.android.com/apk/res/android");

    private const int NameAttributeResourceId = 16842755;
    private const int DebuggableAttributeResourceId = 16842767;
    private const int LegacyStorageAttributeResourceId = 16844291;
    private const int ExtractNativeLibsAttributeResourceId = 16844010;

    private static readonly string[] StandardPermissions =
    [
        "android.permission.READ_EXTERNAL_STORAGE",
        "android.permission.WRITE_EXTERNAL_STORAGE",
        "android.permission.MANAGE_EXTERNAL_STORAGE",
        "android.permission.RECORD_AUDIO", /* used for voice chat in some mods; requires the user to allow it via a prompt */
        "android.permission.INTERNET"
    ];

    private Patcher? _patcher;
    private IPatchLogger? _logger;

    public bool Run(Patcher patcher)
    {
        _patcher = patcher;
        _logger = patcher._logger;

        PatchAPK(patcher._info.OutputBaseApkPath);
        if (patcher._info.OutputLibApkPath != null)
            PatchAPK(patcher._info.OutputLibApkPath);

        return true;
    }

    private void PatchAPK(string apkPath)
    {
        using ZipFile archive = new(apkPath);

        ZipEntry manifestEntry = archive.Entries.First(a => a.FileName == "AndroidManifest.xml");
        using Stream manifestStream = manifestEntry.OpenReader();
        using MemoryStream memoryStream = new();

        manifestStream.CopyTo(memoryStream);
        memoryStream.Position = 0;

        AxmlElement manifest = AxmlLoader.LoadDocument(memoryStream);
        AddStandardPermissions(manifest);
        AddApplicationFlags(manifest);

        using MemoryStream saveStream = new();
        AxmlSaver.SaveDocument(saveStream, manifest);
        saveStream.Position = 0;

        archive.UpdateEntry(manifestEntry.FileName, saveStream);
        archive.Save();
    }

    private void AddStandardPermissions(AxmlElement manifest)
    {
        HashSet<string> existingPermissions = GetExistingChildren(manifest, "uses-permission");

        foreach (string permission in StandardPermissions)
        {
            if (existingPermissions.Contains(permission)) { continue; } // Do not add existing permissions

            _logger!.Log($"Adding permission {permission}");
            AxmlElement permElement = new("uses-permission");
            AddNameAttribute(permElement, permission);
            manifest.Children.Add(permElement);
        }
    }

    private void AddApplicationFlags(AxmlElement manifest)
    {
        AxmlElement appElement = manifest.Children.Single(element => element.Name == "application");
        if (!appElement.Attributes.Any(attribute => attribute.Name == "debuggable"))
        {
            _logger!.Log("Adding debuggable flag");
            appElement.Attributes.Add(new("debuggable", AndroidNamespaceUri, DebuggableAttributeResourceId, true));
        }

        if (!appElement.Attributes.Any(attribute => attribute.Name == "requestLegacyExternalStorage"))
        {
            _logger!.Log("Adding legacy external storage flag");
            appElement.Attributes.Add(new("requestLegacyExternalStorage", AndroidNamespaceUri, LegacyStorageAttributeResourceId, true));
        }

        // This has only been an issue for split APKs
        if (_patcher!._args.IsSplit)
        {
            _logger!.Log("Patching extract native libraries flag");
            AxmlAttribute? extract = appElement.Attributes.FirstOrDefault(attribute => attribute.Name == "extractNativeLibs");
            if (extract != null)
                extract.Value = true;
            else
            {
                appElement.Attributes.Add(new("extractNativeLibs", AndroidNamespaceUri, ExtractNativeLibsAttributeResourceId, true));
            }
        }
    }

    private static void AddNameAttribute(AxmlElement element, string name)
    {
        element.Attributes.Add(new("name", AndroidNamespaceUri, NameAttributeResourceId, name));
    }

    private HashSet<string> GetExistingChildren(AxmlElement manifest, string childNames)
    {
        HashSet<string> result = [];

        foreach (AxmlElement element in manifest.Children)
        {
            if (element.Name != childNames) { continue; }

            List<AxmlAttribute> nameAttributes = element.Attributes.Where(attribute => attribute.Namespace == AndroidNamespaceUri && attribute.Name == "name").ToList();
            // Only add children with the name attribute
            if (nameAttributes.Count > 0) { result.Add((string)nameAttributes[0].Value); }
        }

        return result;
    }
}
