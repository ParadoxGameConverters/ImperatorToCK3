using commonItems;
using System;

namespace ImperatorToCK3 {
    class Program {
        static int Main(string[] args) {
            try {
                Logger.Log(LogLevel.Info, "Hello World!");
                if (args.Length > 0) {
                    Logger.Log(LogLevel.Warning, "ImperatorToCK3 takes no parameters.");
                    Logger.Log(LogLevel.Warning, "It uses configuration.txt, configured manually or by the frontend.");
                }
                Converter.ConvertImperatorToCK3();
                return 0;
            } catch (Exception e) {
                Logger.Log(LogLevel.Error, e.ToString());
                return -1;
            }

        }
    }
}
