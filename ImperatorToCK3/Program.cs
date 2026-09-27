using commonItems;
using commonItems.Exceptions;
using ImperatorToCK3.CommonUtils;
using log4net.Core;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ImperatorToCK3;
public static class Program {
	public static int Main(string[] args) {
		RegisterGlobalExceptionHandlers();
		
		try {
			SetInvariantCulture();
			InstallationDiagnostics.LogEnvironmentContext();

			var converterVersion = new ConverterVersion();
			const string versionPath = "configurables/version.txt";
			try {
				converterVersion.LoadVersion(versionPath);
			} catch (Exception ex) when (ex is UnauthorizedAccessException or IOException) {
				// The converter's own files are unreadable. That is an environment problem, and the
				// stack trace on its own does not say which one, so describe it in full.
				InstallationDiagnostics.LogFileAccessDiagnostics(versionPath, ex);
				throw;
			}
			// LoadVersion reports nothing at all when the file is missing, and ParseFile returns silently
			// when the folder holding it cannot be listed. The failure then resurfaces much later as an
			// unset version in Configuration, blaming something unrelated. Nothing is thrown here on
			// purpose: only the diagnosis is added, the failure itself still reports itself as before.
			InstallationDiagnostics.LogUnreadableFileDiagnostics(versionPath);			Logger.Info(converterVersion.ToString());
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

			// I/O failures elsewhere in the conversion are not necessarily about the converter's own
			// files, so only the parts that need no path are logged here. A caller that already
			// described the exception in full is left alone.
			if (ex is IOException or UnauthorizedAccessException && !InstallationDiagnostics.HasBeenLogged(ex)) {
				InstallationDiagnostics.LogExceptionDiagnostics(ex);
			}

			Logger.Log(Level.Fatal, ex is UserErrorException ? InstallationDiagnostics.Redact(ex.Message) :
				$"{ex.GetType()}: {InstallationDiagnostics.Redact(ex.Message)}");
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

				Logger.Log(Level.Fatal, ex is UserErrorException ? InstallationDiagnostics.Redact(ex.Message) :
					$"{ex.GetType()}: {InstallationDiagnostics.Redact(ex.Message)}");
				if (ex.StackTrace is not null) {
					Logger.Debug(ex.StackTrace);
				}
				// Ensure the process exits with a non-zero code. Should be 1 for user errors and -1 for other exceptions.
				Environment.Exit(ex is UserErrorException ? 1 : -1);
			} else {
				Logger.Log(Level.Fatal, "An unhandled exception occurred, but it could not be identified.");
				Environment.Exit(-1);
			}
		};
		TaskScheduler.UnobservedTaskException += (sender, eventArgs) => {
			Exception ex = eventArgs.Exception;
			// If the exception is an AggregateException, we want the original inner exception's stack trace.
			if (ex is AggregateException aggregateEx) {
				ex = aggregateEx.Flatten().InnerExceptions.FirstOrDefault() ?? ex;
			}

			Logger.Log(Level.Fatal, ex is UserErrorException ? InstallationDiagnostics.Redact(ex.Message) :
				$"{ex.GetType()}: {InstallationDiagnostics.Redact(ex.Message)}");
			if (ex.StackTrace is not null) {
				Logger.Debug(ex.StackTrace);
			}
			// Ensure the process exits with a non-zero code. Should be 1 for user errors and -1 for other exceptions.
			Environment.Exit(ex is UserErrorException ? 1 : -1);
		};
	}

	private static void SetInvariantCulture() {
		CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
		CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
		CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
		CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
	}
}
