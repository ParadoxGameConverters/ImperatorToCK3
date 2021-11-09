using commonItems;
using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace ImperatorToCK3.Outputter {
	public static class VersionOutputter {
		public static void LogConverterVersion(ConverterVersion versionParser) {
			var compileTime = new DateTime(Builtin.CompileTime, DateTimeKind.Utc);
			Logger.Info("************ -= The Paradox Converters Team =- ********************");
			try {
				// read commit id
				string commitId = File.ReadAllText("../commit_id.txt", Encoding.UTF8).Trim();
				Logger.Info("* Converter build based on commit " + commitId);
			} catch {
				Logger.Info("* Converter build based on unknown commit");
			}
			Logger.Info("* " + versionParser.GetDescription());
			Logger.Info("* Built on " + compileTime.ToString("u", CultureInfo.InvariantCulture));
			Logger.Info("*********** + Imperator: Rome To Crusader Kings III + *************\n");
		}
	}
}
