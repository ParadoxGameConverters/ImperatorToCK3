using commonItems;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace ImperatorToCK3.Helpers;

public static class RakalyCaller {
	private const string RakalyVersion = "0.4.0";
	private static readonly string rakalyExecutablePath;
	
	static RakalyCaller() {
		string currentDir = Directory.GetCurrentDirectory();
		rakalyExecutablePath = $"Resources/rakaly/rakaly-{RakalyVersion}-x86_64-pc-windows-msvc/rakaly.exe";
		if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
			rakalyExecutablePath = $"Resources/rakaly/rakaly-{RakalyVersion}-x86_64-apple-darwin/rakaly";
			Exec($"chmod +x {currentDir}/{rakalyExecutablePath}");
		} else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
			rakalyExecutablePath = $"Resources/rakaly/rakaly-{RakalyVersion}-x86_64-unknown-linux-musl/rakaly";
			Exec($"chmod +x {currentDir}/{rakalyExecutablePath}");
		}
	}
	
	public static string GetJson(string filePath) {
		string arguments = $"json --format windows-1252 {filePath}";
		
		using Process process = new();
		process.StartInfo.UseShellExecute = false;
		process.StartInfo.FileName = rakalyExecutablePath;
		process.StartInfo.Arguments = arguments;
		process.StartInfo.CreateNoWindow = true;
		process.StartInfo.RedirectStandardOutput = true;
		process.Start();
		var plainText = process.StandardOutput.ReadToEnd();
		process.WaitForExit();
		var returnCode = process.ExitCode;
		if (returnCode != 0 && returnCode != 1) {
			throw new FormatException($"Rakaly failed to convert {filePath} to JSON with exit code {returnCode}");
		}

		return plainText;
	}
	
	public static void MeltSave(string savePath) {
		string arguments = $"melt --unknown-key stringify \"{savePath}\"";
		
		using Process process = new();
		process.StartInfo.UseShellExecute = false;
		process.StartInfo.FileName = rakalyExecutablePath;
		process.StartInfo.Arguments = arguments;
		process.StartInfo.CreateNoWindow = true;
		process.Start();
		process.WaitForExit();
		var returnCode = process.ExitCode;
		if (returnCode != 0 && returnCode != 1) {
			throw new FormatException($"Rakaly melter failed to melt {savePath} with exit code {returnCode}");
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
				Arguments = $"-c \"{escapedArgs}\""
			}
		};

		process.Start();
		process.WaitForExit();
	}
}