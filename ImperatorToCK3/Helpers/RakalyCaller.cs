using commonItems;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ImperatorToCK3.Helpers {
	public static class RakalyCaller {
		public static void MeltSave(string savePath) {
			string executablePath = "Resources/rakaly/rakaly-0.3.14-x86_64-pc-windows-msvc/rakaly.exe";
			if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
				executablePath = "Resources/rakaly/rakaly-0.3.14-x86_64-apple-darwin/rakaly";
			} else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
				executablePath = "Resources/rakaly/rakaly-0.3.14-x86_64-unknown-linux-musl/rakaly";
			}
			string arguments = $"melt --unknown-key stringify \"{savePath}\"";
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

			var meltedSavePath = CommonFunctions.TrimExtension(savePath) + "_melted.rome";
			System.IO.File.Move(meltedSavePath, "temp/melted_save.rome");
		}
	}
}
