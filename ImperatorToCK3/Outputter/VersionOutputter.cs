using System;
using System.Text;
using System.IO;
using commonItems;

namespace ImperatorToCK3.Outputter {
	public static class VersionOutputter {
		public static void LogConverterVersion(ConverterVersion versionParser) {
			var compileTime = new DateTime(Builtin.CompileTime, DateTimeKind.Utc);
			Logger.Info("************ -= The Paradox Converters Team =- ********************");
			try {
				// read commit id
				string commitID = File.ReadAllText("../commit_id.txt", Encoding.UTF8).Trim();
				Logger.Info("* Converter build based on commit " + commitID);
			} catch {
				Logger.Info("* Converter build based on unknown commit");
			}
			Logger.Info("* " + versionParser.GetDescription());
			Logger.Info("* Built on " + compileTime.ToShortDateString() + " " + compileTime.ToLongTimeString());
			Logger.Info("*********** + Imperator: Rome To Crusader Kings III + *************\n");
		}
	}
}
