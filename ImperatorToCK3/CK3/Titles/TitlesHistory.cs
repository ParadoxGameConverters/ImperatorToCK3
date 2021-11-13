using commonItems;
using ImperatorToCK3.CommonUtils;
using System.Collections.Generic;

namespace ImperatorToCK3.CK3.Titles {
	public class TitlesHistory : Parser {
		public TitlesHistory() { }
		public TitlesHistory(string folderPath, Date ck3BookmarkDate) {
			var filenames = SystemUtils.GetAllFilesInFolderRecursive(folderPath);
			Logger.Info("Parsing title history.");
			RegisterKeys(ck3BookmarkDate);
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

		private void RegisterKeys(Date ck3BookmarkDate) {
			RegisterRegex(@"(e|k|d|c|b)_[A-Za-z0-9_\-\']+", (reader, titleName) => {
				var historyItem = new StringOfItem(reader).String;
				if (historyItem.Contains('{')) {
					var tempReader = new BufferedReader(historyItem);
					if (historyDict.TryGetValue(titleName, out var existingHistory)) {
						existingHistory.Update(historyFactory, tempReader);
					} else {
						var history = historyFactory.GetHistory(tempReader);
						historyDict.Add(titleName, new TitleHistory(history, ck3BookmarkDate));
					}
				}
			});
			RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreItem);
		}
		private readonly HistoryFactory historyFactory = new(
			simpleFieldDefs: new() {
				new() { FieldName = "holder", Setter = "holder", InitialValue = "0" },
				new() { FieldName = "liege", Setter = "liege", InitialValue = null },
				new() { FieldName = "government", Setter = "government", InitialValue = null },
				new() { FieldName = "development_level", Setter = "change_development_level", InitialValue = null },
			},
			containerFieldDefs: new()
		);
		private readonly Dictionary<string, TitleHistory> historyDict = new();
	}
}
