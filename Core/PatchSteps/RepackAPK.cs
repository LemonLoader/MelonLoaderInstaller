﻿using Ionic.Zip;
using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace MelonLoader.Installer.Core.PatchSteps;

internal class RepackAPK : IPatchStep
{
    public bool Run(Patcher patcher)
    {
        patcher._logger.Log("Repacking APK");

        using ZipFile archive = new(patcher._info.OutputBaseApkPath);

        // Handle old installer files
        var dexEntries = archive.Entries.Where(a => a.FileName.Contains("originalDex")).ToArray();
        if (dexEntries.Length > 0)
        {
            patcher._logger.Log("Found reminats of Java patching, replacing patched dex");
            for (int i = dexEntries.Length - 1; i >= 0; i--)
            {
                ZipEntry dex = dexEntries[i];
                string path = dex.FileName;
                dex.Extract(patcher._args.TempDirectory);
                byte[] dexData = File.ReadAllBytes(Path.Combine(patcher._args.TempDirectory, path));

                archive.RemoveEntry(dex);
                archive.RemoveEntry(archive.Entries.First(a => a.FileName == Path.GetFileName(path)));

                archive.AddEntry(Path.GetFileName(path), dexData);
            }

            patcher._logger.Log("Done");
        }

        patcher._logger.Log("Copying data into APK");

        // assets/ data
        CopyTo(archive, Path.Combine(patcher._info.LemonDataDirectory, "MelonLoader"), "assets/MelonLoader");
        CopyTo(archive, Path.Combine(patcher._info.LemonDataDirectory, "dotnet"), "assets/dotnet");
        WritePatchDate(archive);

        // libs data
        if (!patcher._args.IsSplit)
        {
            CopyTo(archive, Path.Combine(patcher._info.LemonDataDirectory, "native"), "lib/arm64-v8a", "*.so");
            CopyTo(archive, Path.Combine(patcher._info.UnityNativeDirectory, "arm64-v8a"), "lib/arm64-v8a", "*.so");
        }
        else
        {
            using ZipFile libArchive = new(patcher._info.OutputLibApkPath);

            CopyTo(libArchive, Path.Combine(patcher._info.LemonDataDirectory, "native"), "lib/arm64-v8a", "*.so");
            CopyTo(libArchive, Path.Combine(patcher._info.UnityNativeDirectory, "arm64-v8a"), "lib/arm64-v8a", "*.so");

            libArchive.Save();
        }

        patcher._logger.Log("Writing, this can take a few");

        archive.Save();

        patcher._logger.Log("Done");

        return true;
    }

    private static void CopyTo(ZipFile archive, string source, string dest, string matcher = "*.*")
    {
        foreach (string file in Directory.GetFiles(source, matcher, SearchOption.AllDirectories))
        {
            string entryPath = Path.Combine(dest, Path.GetRelativePath(source, file)).Replace('\\', '/');

            // I don't think this is supposed to be needed, but I had an issue with an apk having two libmain.so files
            if (archive.ContainsEntry(entryPath))
                archive.RemoveEntry(entryPath);

            archive.AddEntry(entryPath, File.ReadAllBytes(file));
        }
    }

    private static void WritePatchDate(ZipFile archive)
    {
        string entryPath = "assets/lemon_patch_date.txt";
        if (archive.ContainsEntry(entryPath))
            archive.RemoveEntry(entryPath);

        string rfc3339 = XmlConvert.ToString(DateTime.Now, XmlDateTimeSerializationMode.Utc);
        byte[] bytes = Encoding.UTF8.GetBytes(rfc3339);

        archive.AddEntry(entryPath, bytes);
    }
}
