using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using commonItems;

namespace ImperatorToCK3 {
    class Converter {
        public static void ConvertImperatorToCK3(ConverterVersion converterVersion) {
            var config = new Configuration();
			var imperatorWorld = new Imperator.World(config, converterVersion);
			var ck3World = new CK3.World(imperatorWorld, config);
        }

        private void LogGameVersions(string imperatorPath, string ck3Path) {
			// TODO
		}
    }
}
