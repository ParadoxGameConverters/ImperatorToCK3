using commonItems;
using ImperatorToCK3.Exceptions;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace ImperatorToCK3.Helpers;

public static class RakalyCaller {
	private const string RakalyVersion = "0.5.1";
	private static readonly string RelativeRakalyPath;

	static RakalyCaller() {
		string archString = GetArchString();

		string currentDir = Directory.GetCurrentDirectory();
		RelativeRakalyPath = $"Resources/rakaly/rakaly-{RakalyVersion}-{archString}-pc-windows-msvc/rakaly.exe";
		if (OperatingSystem.IsMacOS()) {
			RelativeRakalyPath = $"Resources/rakaly/rakaly-{RakalyVersion}-{archString}-apple-darwin/rakaly";
		} else if (OperatingSystem.IsLinux()) {
			RelativeRakalyPath = $"Resources/rakaly/rakaly-{RakalyVersion}-{archString}-unknown-linux-musl/rakaly";
		}

		if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS()) {
			// Make sure the file is executable.
			var rakalyPath = Path.Combine(currentDir, RelativeRakalyPath).AddQuotes();
			Exec($"chmod +x {rakalyPath}");
		}
	}

	private static string GetArchString() {
		Architecture architecture = RuntimeInformation.OSArchitecture;
		return architecture switch {
			Architecture.X64 => "x86_64",
			Architecture.Arm64 => "aarch64",
			_ => throw new NotSupportedException($"Unsupported architecture: {architecture}")
		};
	}

	public static string GetJson(string filePath) {
		string quotedPath = filePath.AddQuotes();
		string arguments = $"json --format utf-8 {quotedPath}";

		using Process process = new();
		process.StartInfo.UseShellExecute = false;
		process.StartInfo.FileName = RelativeRakalyPath;
		process.StartInfo.Arguments = arguments;
		process.StartInfo.CreateNoWindow = true;
		process.StartInfo.RedirectStandardOutput = true;
		process.Start();
		var plainText = process.StandardOutput.ReadToEnd();
		process.WaitForExit();
		var returnCode = process.ExitCode;
		if (returnCode != 0) {
			throw new FormatException($"Rakaly failed to convert {quotedPath} to JSON with exit code {returnCode}");
		}

		return plainText;
	}

	private static bool IsFileFlaggedAsInfected(Win32Exception ex) {
		// https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-erref/18d8fbe8-a967-4f1c-ae50-99ca8e491d2d
		return ex.NativeErrorCode == 0x000000E1; // ERROR_VIRUS_INFECTED
	}

	private static bool IsFileNotFound(Win32Exception ex) {
		// https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-erref/18d8fbe8-a967-4f1c-ae50-99ca8e491d2d
		return ex.NativeErrorCode == 0x00000002; // ERROR_FILE_NOT_FOUND
	}

	private static void LogWin32ExceptionDetails(Win32Exception ex) {
		Logger.Debug("Message: " + ex.Message);
		Logger.Debug("HResult: " + ex.HResult);
		Logger.Debug("NativeErrorCode: " + ex.NativeErrorCode);
	}

	public static void MeltSave(string savePath) {
		string arguments = $"melt --unknown-key stringify \"{savePath}\"";

		using Process process = new();
		process.StartInfo.UseShellExecute = false;
		process.StartInfo.FileName = RelativeRakalyPath;
		process.StartInfo.Arguments = arguments;
		process.StartInfo.CreateNoWindow = true;
		process.StartInfo.RedirectStandardError = true;

		try {
			process.Start();
			process.WaitForExit();
		}
		catch (Win32Exception e) when (IsFileFlaggedAsInfected(e)) {
			LogWin32ExceptionDetails(e);
			string absoluteRakalyPath = Path.Combine(Directory.GetCurrentDirectory(), RelativeRakalyPath);
			throw new UserErrorException("Failed to run Rakaly because the antivirus blocked it.\n" +
			                             $"Add an exclusion for \"{absoluteRakalyPath}\" to the antivirus and try again.");
		} catch (Win32Exception e) when (IsFileNotFound(e)) {
			LogWin32ExceptionDetails(e);
			throw new UserErrorException("Failed to run Rakaly, it was probably removed by an antivirus.\n" +
			                             "Resave the save in Imperator debug mode and try again.");
		}

		int returnCode = process.ExitCode;
		if (returnCode != 0 && returnCode != 1) {
			Logger.Debug($"Save path: {savePath}");
			if (File.Exists(savePath)) {
				Logger.Debug($"Save file size: {new FileInfo(savePath).Length} bytes");
			}

			Logger.Debug($"Rakaly exit code: {returnCode}");
			string stdErrText = process.StandardError.ReadToEnd();
			Logger.Debug($"Rakaly standard error: {stdErrText}");

			string exceptionMessage = "Rakaly melter failed to melt the save.";
			if (stdErrText.Contains("There is not enough space on the disk.")) {
				throw new UserErrorException($"{exceptionMessage} There is not enough space on the disk.");
			}

			if (stdErrText.Contains("Failed to create melted file")) {
				// Try to copy the file to the converter's temp folder before melting.
				var saveDisk = Path.GetPathRoot(savePath);
				var converterDisk = Path.GetPathRoot(Directory.GetCurrentDirectory());

				if (saveDisk != converterDisk) {
					const string fallbackSavePath = "temp/save_to_be_melted.rome";
					File.Copy(savePath, fallbackSavePath, overwrite: true);
					MeltSave(fallbackSavePath);
					return;
				}
			}

			if (stdErrText.Contains("memory allocation of")) {
				exceptionMessage += " One possible reason is that you don't have enough RAM.";
			}
			throw new FormatException(exceptionMessage);
		}

		var meltedSaveName = $"{CommonFunctions.TrimExtension(savePath)}_melted.rome";
		const string destFileName = "temp/melted_save.rome";
		// first, delete target file if exists, as File.Move() does not support overwrite
		if (File.Exists(destFileName)) {
			File.Delete(destFileName);
		}
		File.Move(meltedSaveName, destFileName);
	}

	// https://stackoverflow.com/a/47918132/10249243
	private static void Exec(string cmd) {
		var escapedArgs = cmd.Replace("\"", "\\\"");

		using var process = new Process {
			StartInfo = new ProcessStartInfo {
				RedirectStandardOutput = true,
				UseShellExecute = false,
				CreateNoWindow = true,
				WindowStyle = ProcessWindowStyle.Hidden,
				FileName = "/bin/bash",
				Arguments = $"-c \"{escapedArgs}\"",
			},
		};

		process.Start();
		process.WaitForExit();

		var stdOut = process.StandardOutput.ReadToEnd().Trim();
		if (!string.IsNullOrEmpty(stdOut)) {
			Logger.Debug("Exec output: " + stdOut);
		}
	}
}