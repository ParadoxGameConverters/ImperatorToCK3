using commonItems;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace ImperatorToCK3.Helpers {
	public static class RakalyCaller {
		public static void MeltSave(string savePath) {
			using Process process = new();

			string currentDir = Directory.GetCurrentDirectory();
			string executablePath = "Resources/rakaly/rakaly-0.3.15-x86_64-pc-windows-msvc/rakaly.exe";
			string arguments = $"melt --unknown-key stringify \"{savePath}\"";
			if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
				executablePath = "Resources/rakaly/rakaly-0.3.15-x86_64-apple-darwin/rakaly";
				process.StartInfo.UseShellExecute = true;
				Exec($"chmod +x {currentDir}/{executablePath}");
			} else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
				executablePath = "Resources/rakaly/rakaly-0.3.15-x86_64-unknown-linux-musl/rakaly";
				Exec($"chmod +x {currentDir}/{executablePath}");
			}

			process.StartInfo.UseShellExecute = false;
			process.StartInfo.FileName = executablePath;
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
			//first, delete target file if exists, as File.Move() does not support overwrite
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
}
