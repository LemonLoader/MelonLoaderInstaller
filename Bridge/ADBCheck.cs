using System.Diagnostics;
using System.IO.Compression;

namespace LemonADBBridge
{
    public static class ADBCheck
    {
        public static async Task CheckAndExtract()
        {
            string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "LemonADB");
            StaticStuff.ADBPath = Path.Combine(folderPath, "adb.exe");
            if (!File.Exists(StaticStuff.ADBPath))
            {
                using MemoryStream stream = new MemoryStream(Resources.platform_tools, false);
                await UnzipFromStream(new ZipArchive(stream), folderPath);
            }
        }

        private static async Task UnzipFromStream(ZipArchive zip, string outFolder)
        {
            if (!Directory.Exists(outFolder))
                Directory.CreateDirectory(outFolder);

            using (zip)
            {
                foreach (var entry in zip.Entries)
                {
                    var fullZipToPath = Path.Combine(outFolder, entry.FullName);
                    using var stream = entry.Open();
                    using FileStream fileStream = File.Create(fullZipToPath);
                    await stream.CopyToAsync(fileStream);
                }
            }
        }
    }
}
