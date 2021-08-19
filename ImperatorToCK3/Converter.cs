using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using commonItems;

namespace ImperatorToCK3 {
    class Converter {
        public static void ConvertImperatorToCK3() {
            var config = new Configuration();
            var plainText = Helpers.RakalyCaller.ToPlainText(config.SaveGamePath);
            Logger.Log(LogLevel.Debug, plainText.Split('\n').Length.ToString());
        }

        private void LogGameVersions(string imperatorPath, string ck3Path) {

        }
    }
}
