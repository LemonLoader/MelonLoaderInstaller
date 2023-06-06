using System.IO.Compression;
using System.IO;
using System.Text.RegularExpressions;

namespace MelonLoaderInstaller.Core.PatchSteps
{
    internal class RemoveStaleFiles : IPatchStep
    {
        public bool Run(Patcher patcher)
        {
            patcher._logger.Log("Removing old files");

            Regex[] patterns = new Regex[]
            {
                //new Regex("^classes\\d*\\.dex$", RegexOptions.Multiline),
                new Regex("^META-INF\\/.*", RegexOptions.Multiline),
                new Regex("^lib\\/.*\\/libunity\\.so", RegexOptions.Multiline),
            };

            using FileStream apkStream = new FileStream(patcher._info.OutputBaseApkPath, FileMode.Open);
            using ZipArchive archive = new ZipArchive(apkStream, ZipArchiveMode.Update);

            for (int i = archive.Entries.Count - 1; i >= 0; i--)
            {
                ZipArchiveEntry file = archive.Entries[i];
                foreach (Regex regex in patterns)
                {
                    if (regex.IsMatch(file.FullName))
                    {
                        file.Delete();
                    }
                }
            }

            return true;
        }
    }
}
