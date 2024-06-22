using System.Text;

namespace MelonLoader.Installer.Core.Utilities
{
    // Auto-converted from https://pastebin.com/c53DuqMt (then I obliterated it), writer unknown
    // I do not know how this code works, please do not ask
    internal class ABXTools
    {
        private const int START_TAG = 0x00100102;

        /// <summary>
        /// Reads the ABX and sets extractNativeLibs to true
        /// </summary>
        /// <param name="xml">The unmodified ABX byte data</param>
        /// <returns>The modified ABX byte data</returns>
        public static byte[] EnableExtractNativeLibs(byte[] xml)
        {
            int numbStrings = LEW(xml, 4 * 4);
            int sitOff = 0x24;
            int stOff = sitOff + numbStrings * 4;
            int xmlTagOff = LEW(xml, 3 * 4);

            for (int ii = xmlTagOff; ii < xml.Length - 4; ii += 4)
            {
                if (LEW(xml, ii) == START_TAG)
                {
                    xmlTagOff = ii;
                    break;
                }
            }

            int off = xmlTagOff;
            while (off < xml.Length)
            {
                int tag0 = LEW(xml, off);

                if (tag0 != START_TAG) break;

                int numbAttrs = LEW(xml, off + 7 * 4);
                off += 9 * 4;

                for (int ii = 0; ii < numbAttrs; ii++)
                {
                    int attrNameSi = LEW(xml, off + 1 * 4);
                    int attrResId = LEW(xml, off + 4 * 4);

                    string attrName = CompXmlString(xml, sitOff, stOff, attrNameSi);

                    if (attrName == "extractNativeLibs" && attrResId == 0)
                    {
                        xml[off + 4 * 4] = 255;
                        return xml;
                    }

                    off += 5 * 4;
                }
            }

            return xml;
        }

        /// <summary>
        /// I do not know what this does
        /// </summary>
        public static string CompXmlString(byte[] xml, int sitOff, int stOff, int strInd)
        {
            if (strInd < 0)
                return string.Empty;
            int strOff = stOff + LEW(xml, sitOff + strInd * 4);
            return CompXmlStringAt(xml, strOff);
        }

        /// <summary>
        /// Return the string stored in StringTable format at offset strOff. This offset points to the 16 bit string length, which is followed by that number of 16 bit (Unicode) chars.
        /// </summary>
        public static string CompXmlStringAt(byte[] arr, int strOff)
        {
            int strLen = arr[strOff + 1] << 8 & 0xff00 | arr[strOff] & 0xff;
            byte[] chars = new byte[strLen];
            for (int ii = 0; ii < strLen; ii++)
                chars[ii] = arr[strOff + 2 + ii * 2];
            return Encoding.UTF8.GetString(chars);
        }

        /// <summary>
        /// Return value of a Little Endian 32 bit word from the byte array at offset off.
        /// </summary>
        private static int LEW(byte[] arr, int off) => arr[off + 3] << 24 & unchecked((int)0xff000000) | arr[off + 2] << 16 & 0xff0000 | arr[off + 1] << 8 & 0xff00 | arr[off] & 0xFF;
    }
}
