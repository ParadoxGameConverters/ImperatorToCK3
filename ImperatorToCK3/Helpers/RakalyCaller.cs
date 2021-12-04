using commonItems;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace ImperatorToCK3.Helpers {
	public static class RakalyCaller {
		public static void MeltSave(string savePath) {
			string executablePath = "Resources/rakaly/rakaly-0.3.15-x86_64-pc-windows-msvc/rakaly.exe";
			string arguments = $"melt --unknown-key stringify \"{savePath}\"";
			if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
				executablePath = "Resources/rakaly/rakaly-0.3.15-x86_64-apple-darwin/rakaly";
			} else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
				executablePath = "Resources/rakaly/rakaly-0.3.15-x86_64-unknown-linux-musl/rakaly";
				arguments = $"sudo {arguments}";
			}
			using Process process = new();
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
	}
}
