using System.IO.Compression;
using System.IO;
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

            using FileStream apkStream = new FileStream(patcher._info.OutputBaseApkPath, FileMode.Open);
            using ZipArchive archive = new ZipArchive(apkStream, ZipArchiveMode.Update);

            ZipArchiveEntry manifestEntry = archive.GetEntry("AndroidManifest.xml");
            using Stream manifestStream = manifestEntry.Open();
            using MemoryStream memoryStream = new MemoryStream();

            manifestStream.CopyTo(memoryStream);

            byte[] patchedManifest = ABXTools.EnableExtractNativeLibs(memoryStream.ToArray());

            manifestStream.SetLength(0);
            manifestStream.Write(patchedManifest, 0, patchedManifest.Length);

            return true;
        }
    }
}
