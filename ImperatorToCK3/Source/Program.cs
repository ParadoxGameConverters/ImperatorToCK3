using commonItems;
using System;

namespace ImperatorToCK3
{
    class Program
    {
        static int Main(string[] args)
        {
            try
            {
                Log.WriteLine(LogLevel.Info, "Hello World!");
                if (args.Length > 0)
                {
                    Log.WriteLine(LogLevel.Warning, "ImperatorToCK3 takes no parameters.");
                    Log.WriteLine(LogLevel.Warning, "It uses configuration.txt, configured manually or by the frontend.");
                }
                Converter.ConvertImperatorToCK3();
                return 0;
            }
            catch (Exception e)
            {
                Log.WriteLine(LogLevel.Error, e.ToString());
                return -1;
            }

        }
    }
}
