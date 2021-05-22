using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace commonItems
{
    class SystemUtils
    {
        SortedSet<string> GetAllFilesInFolder(string path)
        {
            return new SortedSet<string>(Directory.GetFiles(path));
        }
    }
}
