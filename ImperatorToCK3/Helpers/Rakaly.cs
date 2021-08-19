using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace ImperatorToCK3.Helpers
{

    public static class RakalyCaller
    {
        public static string ToPlainText(string filePath)
        {
            string executableName = "rakaly";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                executableName += ".exe";
            }
            string arguments = $"melt --unknown-key stringify --to-stdout \"{filePath}\"";
            using Process process = new();
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.FileName = executableName;
            process.StartInfo.Arguments = arguments;
            process.StartInfo.CreateNoWindow = true;
            process.Start();
            // This code assumes the process you are starting will terminate itself.
            // Given that is is started without a window so you cannot terminate it
            // on the desktop, it must terminate itself or you can do it programmatically
            // from this application using the Kill method.
            process.WaitForExit();
            var plainTextSave = process.StandardOutput.ReadToEnd();
            var returnCode = process.ExitCode;
            if (returnCode != 0 && returnCode != 1)
            {
                throw new ApplicationException($"Rakaly melter failed to melt {filePath} with exit code {returnCode}");
            }
            return plainTextSave;
        }
    }
}
