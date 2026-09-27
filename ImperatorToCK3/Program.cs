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

			var converterVersion = LoadConverterVersion();
			WarnAboutUnexpectedParameters(args);

			Converter.ConvertImperatorToCK3(converterVersion);
			return 0;
		} catch (Exception ex) {
			ex = UnwrapAggregateException(ex);
			LogFatalError(ex);
			return GetExitCode(ex);
		}
	}

	/// <summary>
	/// Loads the converter's own version file, describing the installation in full if it turns out
	/// that the converter cannot read it.
	/// </summary>
	private static ConverterVersion LoadConverterVersion() {
		const string versionPath = "configurables/version.txt";
		var converterVersion = new ConverterVersion();
		try {
			converterVersion.LoadVersion(versionPath);
		} catch (Exception ex) when (ex is UnauthorizedAccessException or IOException) {
			// The converter's own files are unreadable. That is an environment problem, and the stack
			// trace on its own does not say which one, so describe it in full.
			InstallationDiagnostics.LogFileAccessDiagnostics(versionPath, ex);
			throw;
		}

		// LoadVersion reports nothing at all when the file is missing, and ParseFile returns silently
		// when the folder holding it cannot be listed. The failure then resurfaces much later as an
		// unset version in Configuration, blaming something unrelated. Nothing is thrown here on
		// purpose: only the diagnosis is added, the failure itself still reports itself as before.
		InstallationDiagnostics.LogUnreadableFileDiagnostics(versionPath);
		Logger.Info(converterVersion.ToString());
		return converterVersion;
	}

	private static void WarnAboutUnexpectedParameters(string[] args) {
		if (args.Length == 0) {
			return;
		}
		Logger.Warn("ImperatorToCK3 takes no parameters.\n" +
		            "It uses configuration.txt, configured manually or by the frontend.");
	}

	private static void RegisterGlobalExceptionHandlers() {
		// Catch any unhandled exceptions from other threads.
		AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) => {
			if (eventArgs.ExceptionObject is not Exception ex) {
				Logger.Log(Level.Fatal, "An unhandled exception occurred, but it could not be identified.");
				Environment.Exit(-1);
				return;
			}
			ex = UnwrapAggregateException(ex);
			LogFatalError(ex);
			// Ensure the process exits with a non-zero code. Should be 1 for user errors and -1 for other exceptions.
			Environment.Exit(GetExitCode(ex));
		};
		TaskScheduler.UnobservedTaskException += (sender, eventArgs) => {
			Exception ex = UnwrapAggregateException(eventArgs.Exception);
			LogFatalError(ex);
			// Ensure the process exits with a non-zero code. Should be 1 for user errors and -1 for other exceptions.
			Environment.Exit(GetExitCode(ex));
		};
	}

	/// <summary>Reports a failure that ends the conversion, whatever raised it.</summary>
	private static void LogFatalError(Exception ex) {
		// I/O failures are not necessarily about the converter's own files, so only the parts that
		// need no path are logged here. A caller that already described the exception in full is left
		// alone.
		if (ex is IOException or UnauthorizedAccessException && !InstallationDiagnostics.HasBeenLogged(ex)) {
			InstallationDiagnostics.LogExceptionDiagnostics(ex);
		}

		Logger.Log(Level.Fatal, ex is UserErrorException ? InstallationDiagnostics.Redact(ex.Message) :
			$"{ex.GetType()}: {InstallationDiagnostics.Redact(ex.Message)}");
		if (ex.StackTrace is not null) {
			Logger.Debug(ex.StackTrace);
		}
	}

	/// <summary>
	/// If the exception is an AggregateException, we want the original inner exception's stack trace.
	/// </summary>
	private static Exception UnwrapAggregateException(Exception ex) {
		return ex is AggregateException aggregateEx
			? aggregateEx.Flatten().InnerExceptions.FirstOrDefault() ?? ex
			: ex;
	}

	/// <summary>
	/// Return exit code 1 for user errors. They should not be reported to Sentry. Any other failure is
	/// a converter bug or a broken environment, and is reported as -1.
	/// </summary>
	private static int GetExitCode(Exception ex) => ex is UserErrorException ? 1 : -1;

	private static void SetInvariantCulture() {
		CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
		CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
		CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
		CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
	}
}
