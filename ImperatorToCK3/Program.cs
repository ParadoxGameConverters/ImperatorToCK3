using commonItems;
using ImperatorToCK3.Exceptions;
using log4net.Core;
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace ImperatorToCK3;
public static class Program {
	public static int Main(string[] args) {
		RegisterGlobalExceptionHandlers();
		
		try {
			SetInvariantCulture();

			var converterVersion = new ConverterVersion();
			converterVersion.LoadVersion("configurables/version.txt");
			Logger.Info(converterVersion.ToString());
			if (args.Length > 0) {
				Logger.Warn("ImperatorToCK3 takes no parameters.\n" +
				            "It uses configuration.txt, configured manually or by the frontend.");
			}
			Converter.ConvertImperatorToCK3(converterVersion);
			return 0;
		} catch (Exception ex) {
			// If the exception is an AggregateException, we want the original inner exception's stack trace.
			if (ex is AggregateException aggregateEx) {
				ex = aggregateEx.Flatten().InnerExceptions.FirstOrDefault() ?? ex;
			}

			Logger.Log(Level.Fatal, ex is UserErrorException ? ex.Message : $"{ex.GetType()}: {ex.Message}");
			if (ex.StackTrace is not null) {
				Logger.Debug(ex.StackTrace);
			}

			// Return exit code 1 for user errors. They should not be reported to Sentry.
			if (ex is UserErrorException) {
				return 1;
			}
			return -1;
		}
	}

	private static void RegisterGlobalExceptionHandlers() {
		// Catch any unhandled exceptions from other threads.
		AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) => {
			if (eventArgs.ExceptionObject is Exception ex) {
				// If the exception is an AggregateException, we want the original inner exception's stack trace.
				if (ex is AggregateException aggregateEx) {
					ex = aggregateEx.Flatten().InnerExceptions.FirstOrDefault() ?? ex;
				}

				Logger.Log(Level.Fatal, ex is UserErrorException ? ex.Message : $"{ex.GetType()}: {ex.Message}");
				if (ex.StackTrace is not null) {
					Logger.Debug(ex.StackTrace);
				}
			} else {
				Logger.Log(Level.Fatal, "An unhandled exception occurred, but it could not be identified.");
			}
			Environment.Exit(-1); // Ensure the process exits with a non-zero code.
		};
		TaskScheduler.UnobservedTaskException += (sender, eventArgs) => {
			Exception ex = eventArgs.Exception;
			// If the exception is an AggregateException, we want the original inner exception's stack trace.
			if (ex is AggregateException aggregateEx) {
				ex = aggregateEx.Flatten().InnerExceptions.FirstOrDefault() ?? ex;
			}

			Logger.Log(Level.Fatal, ex is UserErrorException ? ex.Message : $"{ex.GetType()}: {ex.Message}");
			if (ex.StackTrace is not null) {
				Logger.Debug(ex.StackTrace);
			}
			Environment.Exit(-1); // Ensure the process exits with a non-zero code.
		};

	}

	private static void SetInvariantCulture() {
		CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
		CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
		CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
		CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
	}
}
