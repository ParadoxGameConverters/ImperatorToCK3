using commonItems;
using System;
using System.Globalization;

namespace ImperatorToCK3 {
	public static class Program {
		public static int Main(string[] args) {
			try {
				CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
				CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
				CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
				CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
				
				var converterVersion = new ConverterVersion();
				converterVersion.LoadVersion("configurables/version.txt");
				Logger.Info(converterVersion.ToString());
				if (args.Length > 0) {
					Logger.Warn("ImperatorToCK3 takes no parameters.\n" +
					            "It uses configuration.txt, configured manually or by the frontend.");
				}
				Converter.ConvertImperatorToCK3(converterVersion);
				return 0;
			} catch (Exception e) {
				Logger.Error($"{e.GetType()}: {e.Message}");
				if (e.StackTrace is not null) {
					Logger.Debug(e.StackTrace);
				}
				return -1;
			}
		}
	}
}
