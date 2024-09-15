using commonItems;
using CWTools.CSharp;
using CWTools.Parser;
using CWTools.Process;
using ImperatorToCK3.CK3;
using ImperatorToCK3.Exceptions;
using log4net.Core;
using Microsoft.FSharp.Collections;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace ImperatorToCK3;
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
			Logger.Log(Level.Fatal, e is UserErrorException ? e.Message : $"{e.GetType()}: {e.Message}");
			if (e.StackTrace is not null) {
				Logger.Debug(e.StackTrace);
			}

			// Return exit code 1 for user errors. They should not be reported to Sentry.
			if (e is UserErrorException) {
				return 1;
			}
			return -1;
		}
	}
}
