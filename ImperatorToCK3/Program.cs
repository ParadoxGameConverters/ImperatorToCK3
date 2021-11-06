using commonItems;
using ImperatorToCK3.Outputter;
using System;

namespace ImperatorToCK3 {
	class Program {
		static int Main(string[] args) {
			try {
				var converterVersion = new ConverterVersion();
				converterVersion.LoadVersion("configurables/version.txt");
				VersionOutputter.LogConverterVersion(converterVersion);
				if (args.Length > 0) {
					Logger.Warn("ImperatorToCK3 takes no parameters.");
					Logger.Warn("It uses configuration.txt, configured manually or by the frontend.");
				}
				Converter.ConvertImperatorToCK3(converterVersion);
				return 0;
			} catch (Exception e) {
				Logger.Error(e.ToString());
				return -1;
			}
		}
	}
}
