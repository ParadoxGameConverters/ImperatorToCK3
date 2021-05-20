using commonItems;
using System;
using System.IO;

namespace ImperatorToCK3
{
    class Program
    {
        static void Main(string[] args)
        {
            Log.WriteLine(LogLevel.Info, "Hello World!");
            if (args.Length > 0)
            {
                Log.WriteLine(LogLevel.Warning, "ImperatorToCK3 takes no parameters.");
                Log.WriteLine(LogLevel.Warning, "It uses configuration.txt, configured manually or by the frontend.");
            }
        }
    }
}
