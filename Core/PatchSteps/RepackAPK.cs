using System;
using System.IO.Compression;
using System.IO;

namespace MelonLoaderInstaller.Core.PatchSteps
{
    internal class RepackAPK : IPatchStep
    {
        public bool Run(Patcher patcher)
        {
            using FileStream apkStream = new FileStream(patcher._info.OutputBaseApkPath, FileMode.Open);
            using ZipArchive archive = new ZipArchive(apkStream, ZipArchiveMode.Update);

            // assets/melonloader data
            CopyTo(archive, Path.Combine(patcher._info.LemonDataDirectory, "core"), "assets/melonloader/etc", "*.dll");
            CopyTo(archive, Path.Combine(patcher._info.LemonDataDirectory, "managed"), "assets/melonloader/etc/managed", "*.dll");
            CopyTo(archive, Path.Combine(patcher._info.LemonDataDirectory, "mono", "bcl"), "assets/melonloader/etc/managed", "*.dll");
            CopyTo(archive, Path.Combine(patcher._info.LemonDataDirectory, "support_modules"), "assets/melonloader/etc/support", "*.dll");
            CopyTo(archive, Path.Combine(patcher._info.LemonDataDirectory, "assembly_generation"), "assets/melonloader/etc/assembly_generation/managed", "*.dll");

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
                using FileStream libApkStream = new FileStream(patcher._info.OutputLibApkPath, FileMode.Open);
                using ZipArchive libArchive = new ZipArchive(libApkStream, ZipArchiveMode.Update);

                CopyTo(libArchive, Path.Combine(patcher._info.LemonDataDirectory, "native"), "lib/arm64-v8a", "*.so");
                CopyTo(libArchive, Path.Combine(patcher._info.UnityNativeDirectory, "arm64-v8a"), "lib/arm64-v8a", "*.so");
            }

            return true;
        }

        private void CopyTo(ZipArchive archive, string source, string dest, string matcher = "*.*")
        {
            foreach (string file in Directory.GetFiles(source, matcher, SearchOption.AllDirectories))
            {
                string entryPath = Path.Combine(dest, Path.GetRelativePath(source, file)).Replace('\\', '/');
                Console.WriteLine(entryPath);
                archive.CreateEntryFromFile(file, entryPath);
            }
        }
    }
}
