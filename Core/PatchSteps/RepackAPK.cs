using Ionic.Zip;
using Org.BouncyCastle.Asn1.Ocsp;
using System;
using System.Data;
using System.IO;
using System.Linq;

namespace MelonLoader.Installer.Core.PatchSteps
{
    internal class RepackAPK : IPatchStep
    {
        public bool Run(Patcher patcher)
        {
            using ZipFile archive = new ZipFile(patcher._info.OutputBaseApkPath);

            // Handle old installer files
            var dexEntries = archive.Entries.Where(a => a.FileName.Contains("originalDex")).ToArray();
            if (dexEntries.Count() > 0)
            {
                patcher._logger.Log("Found reminats of Java patching, replacing patched dex.");
                for (int i = dexEntries.Count() - 1; i >= 0; i--)
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

            // assets/melonloader data
            CopyTo(archive, Path.Combine(patcher._info.LemonDataDirectory, "core"), "assets/melonloader/etc", "*.dll");
            CopyTo(archive, Path.Combine(patcher._info.LemonDataDirectory, "managed"), "assets/melonloader/etc/managed", "*.dll");
            CopyTo(archive, Path.Combine(patcher._info.LemonDataDirectory, "mono", "bcl"), "assets/melonloader/etc/managed", "*.dll");
            CopyTo(archive, Path.Combine(patcher._info.LemonDataDirectory, "support_modules"), "assets/melonloader/etc/support", "*.dll");
            CopyTo(archive, Path.Combine(patcher._info.LemonDataDirectory, "assembly_generation"), "assets/melonloader/etc/assembly_generation/managed", "*.dll");
            CopyTo(archive, Path.Combine(patcher._info.UnityManagedDirectory), "assets/melonloader/etc/assembly_generation/unity", "*.dll");

            // assets/bin data
            CopyTo(archive, Path.Combine(patcher._info.LemonDataDirectory, "etc"), "assets/bin/Data/Managed/etc");

            // libs data
            if (!patcher._args.IsSplit)
            {
                CopyTo(archive, Path.Combine(patcher._info.LemonDataDirectory, "native"), "lib/arm64-v8a", "*.so");
                CopyTo(archive, Path.Combine(patcher._info.UnityNativeDirectory, "arm64-v8a"), "lib/arm64-v8a", "*.so");
            }
            else
            {
                using ZipFile libArchive = new ZipFile(patcher._info.OutputLibApkPath);

                CopyTo(libArchive, Path.Combine(patcher._info.LemonDataDirectory, "native"), "lib/arm64-v8a", "*.so");
                CopyTo(libArchive, Path.Combine(patcher._info.UnityNativeDirectory, "arm64-v8a"), "lib/arm64-v8a", "*.so");

                libArchive.Save();
            }

            archive.Save();

            return true;
        }

        private void CopyTo(ZipFile archive, string source, string dest, string matcher = "*.*")
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
    }
}
