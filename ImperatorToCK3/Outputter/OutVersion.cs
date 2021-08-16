using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using commonItems;

namespace ImperatorToCK3.Outputter {
	public static class VersionOutputter {
		public static void LogConverterVersion(ConverterVersion versionParser) {
			var compileTime = new DateTime(Builtin.CompileTime, DateTimeKind.Utc);
			// read commit id
			string commitID = File.ReadAllText("../commit_id.txt", Encoding.UTF8).Trim();
			Logger.Log(LogLevel.Info, "************ -= The Paradox Converters Team =- ********************");
			Logger.Log(LogLevel.Info, "* Converter build based on commit " + commitID);
			Logger.Log(LogLevel.Info, "* " + versionParser.GetDescription());
			Logger.Log(LogLevel.Info, "* Built on " + compileTime.ToShortDateString() + " " + compileTime.ToLongTimeString());
			Logger.Log(LogLevel.Info, "*********** + Imperator: Rome To Crusader Kings III + *************\n");
		}
	}
}
