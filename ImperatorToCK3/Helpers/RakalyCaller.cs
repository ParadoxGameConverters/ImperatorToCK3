using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Diagnostics;
using commonItems;

namespace ImperatorToCK3.Helpers
{

    public static class RakalyCaller
    {
        public static string ToPlainText(string savePath)
        {
            string executablePath = "Resources/rakaly-0.3.12-x86_64-pc-windows-msvc/rakaly.exe";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                executablePath = "Resources/rakaly-0.3.12-x86_64-apple-darwin/rakaly";
            } else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                executablePath = "Resources/rakaly-0.3.12-x86_64-unknown-linux-musl/rakaly";
            }
            string arguments = $"melt --unknown-key stringify --to-stdout \"{savePath}\"";
            using Process process = new();
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.FileName = executablePath;
            process.StartInfo.Arguments = arguments;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.Start();

            var plainText = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            var returnCode = process.ExitCode;
            if (returnCode != 0 && returnCode != 1)
            {
                throw new ApplicationException($"Rakaly melter failed to melt {savePath} with exit code {returnCode}");
            }
            return plainText;
        }
    }
}
