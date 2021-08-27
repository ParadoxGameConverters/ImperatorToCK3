using System.Collections.Generic;
using commonItems;
using ImperatorToCK3.CommonUtils;

namespace ImperatorToCK3.CK3.Titles {
	public class TitlesHistory : Parser {
		public TitlesHistory() { }
		public TitlesHistory(string folderPath) {
			var filenames = SystemUtils.GetAllFilesInFolderRecursive(folderPath);
			Logger.Info("Parsing title history.");
			RegisterKeys();
			foreach (var filename in filenames) {
				ParseFile(System.IO.Path.Combine(folderPath, filename));
			}
			ClearRegisteredRules();
			Logger.Info($"Loaded {historyDict.Count} title histories.");
		}
		public TitleHistory? PopTitleHistory(string titleName) { // "pop" as from stack, not Imperator Pop ;)
			if (historyDict.TryGetValue(titleName, out var historyToReturn)) {
				historyDict.Remove(titleName);
				return historyToReturn;
			}
			return null;
		}

		private void RegisterKeys() {
			RegisterRegex(@"(e|k|d|c|b)_[A-Za-z0-9_\-\']+", (reader, titleName) => {
				var historyItem = new StringOfItem(reader).String;
				if (historyItem.IndexOf('{') != -1) {
					var tempReader = new BufferedReader(historyItem);
					var history = historyFactory.GetHistory(tempReader);
					historyDict.Add(titleName, new TitleHistory(history));
				}
			});
			RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreItem);
		}
		private readonly HistoryFactory historyFactory = new(
			simpleFieldDefs: new() {
				new() { FieldName = "holder", Setter = "holder", InitialValue = "0" },
				new() { FieldName = "liege", Setter = "liege", InitialValue = null },
				new() { FieldName = "government", Setter = "government", InitialValue = null },
				new() { FieldName = "development_level", Setter = "development_level", InitialValue = null },
			},
			containerFieldDefs: new()
		);
		private readonly Dictionary<string, TitleHistory> historyDict = new();
	}
}
