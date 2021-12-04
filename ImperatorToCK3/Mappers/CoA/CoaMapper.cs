using commonItems;
using System.Collections.Generic;
using System.IO;

namespace ImperatorToCK3.Mappers.CoA {
	public class CoaMapper {
		public CoaMapper() { }
		public CoaMapper(Configuration theConfiguration) {
			var coasPath = Path.Combine(theConfiguration.ImperatorPath, "game", "common", "coat_of_arms", "coat_of_arms");
			var fileNames = SystemUtils.GetAllFilesInFolderRecursive(coasPath);
			Logger.Info("Parsing CoAs...");
			var parser = new Parser();
			RegisterKeys(parser);
			foreach (var fileName in fileNames) {
				parser.ParseFile(Path.Combine(coasPath, fileName));
			}
			Logger.Info($"Loaded {coasMap.Count} coats of arms.");
		}
		public CoaMapper(string coaFilePath) {
			var parser = new Parser();
			RegisterKeys(parser);
			parser.ParseFile(coaFilePath);
		}
		private void RegisterKeys(Parser parser) {
			parser.RegisterKeyword("template", ParserHelpers.IgnoreItem); // we don't need templates, we need CoAs!
			parser.RegisterRegex(CommonRegexes.Catchall, (reader, flagName) => coasMap.Add(flagName, reader.GetStringOfItem().ToString()));
		}

		public string? GetCoaForFlagName(string impFlagName) {
			bool contains = coasMap.TryGetValue(impFlagName, out string? value);
			return contains ? value : null;
		}

		private readonly Dictionary<string, string> coasMap = new();
	}
}
