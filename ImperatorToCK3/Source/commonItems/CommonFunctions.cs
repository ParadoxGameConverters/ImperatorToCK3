using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace commonItems
{
    class CommonFunctions
    {
        public static string TrimPath(string filename)
        {
            var lastSlash = filename.LastIndexOf('\\');
            var trimmedFileName = filename.Substring(lastSlash + 1, filename.Length);
            lastSlash = trimmedFileName.LastIndexOf('/');
            trimmedFileName = trimmedFileName.Substring(lastSlash + 1, trimmedFileName.Length);
            return trimmedFileName;
        }

        public static string TrimExtenstion(string filename)
        {
            var rawFile = TrimPath(filename);
            var dotPos = rawFile.LastIndexOf('.');
            if (dotPos == -1)
            {
                return "";
            }
            else
            {
                return rawFile.Substring(dotPos + 1);
            }
        }

        public static string NormalizeUTF8Path(string utf8Path)
        {
            string asciiPath = EncodingConversions.ConvertUTF8ToASCII(utf8Path);
            asciiPath = asciiPath.Replace('/', '_');
            asciiPath = asciiPath.Replace('\\', '_');
            asciiPath = asciiPath.Replace(':', '_');
            asciiPath = asciiPath.Replace('*', '_');
            asciiPath = asciiPath.Replace('?', '_');
            asciiPath = asciiPath.Replace('\"', '_');
            asciiPath = asciiPath.Replace('<', '_');
            asciiPath = asciiPath.Replace('>', '_');
            asciiPath = asciiPath.Replace('|', '_');
            asciiPath = asciiPath.Replace("\t", string.Empty);

            return asciiPath;
        }
    }
}
