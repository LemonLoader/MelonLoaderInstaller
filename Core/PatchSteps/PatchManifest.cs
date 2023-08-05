using System.IO;
using System.Linq;
using Ionic.Zip;
using MelonLoaderInstaller.Core.Utilities;

namespace MelonLoaderInstaller.Core.PatchSteps
{
    internal class PatchManifest : IPatchStep
    {
        public bool Run(Patcher patcher)
        {
            // Only needed for split APKs
            if (!patcher._args.IsSplit)
                return true;

            // base.apk
            {
                using ZipFile archive = new ZipFile(patcher._info.OutputBaseApkPath);

                ZipEntry manifestEntry = archive.Entries.First(a => a.FileName == "AndroidManifest.xml");
                using Stream manifestStream = manifestEntry.OpenReader();
                using MemoryStream memoryStream = new MemoryStream();

                manifestStream.CopyTo(memoryStream);

                byte[] patchedManifest = ABXTools.EnableExtractNativeLibs(memoryStream.ToArray());

                archive.UpdateEntry(manifestEntry.FileName, patchedManifest);

                archive.Save();
            }

            // split_config.*.apk
            {
                using ZipFile archive = new ZipFile(patcher._info.OutputLibApkPath);

                ZipEntry manifestEntry = archive.Entries.First(a => a.FileName == "AndroidManifest.xml");
                using Stream manifestStream = manifestEntry.OpenReader();
                using MemoryStream memoryStream = new MemoryStream();

                manifestStream.CopyTo(memoryStream);

                byte[] patchedManifest = ABXTools.EnableExtractNativeLibs(memoryStream.ToArray());

                archive.UpdateEntry(manifestEntry.FileName, patchedManifest);

                archive.Save();
            }

            return true;
        }
    }
}
