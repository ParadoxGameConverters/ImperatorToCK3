﻿using commonItems;
using commonItems.Collections;
using ImperatorToCK3.CommonUtils;
using System.Collections.Generic;

namespace ImperatorToCK3.CK3.Titles;

public class TitlesHistory : Parser {
	public TitlesHistory() { }
	public TitlesHistory(string folderPath) {
		var filenames = SystemUtils.GetAllFilesInFolderRecursive(folderPath);
		Logger.Info("Parsing title history...");
		RegisterKeys();
		foreach (var filename in filenames) {
			ParseFile(System.IO.Path.Combine(folderPath, filename));
		}
		ClearRegisteredRules();
		Logger.Info($"Loaded {historyDict.Count} title histories.");
	}
	public TitleHistory? PopTitleHistory(string titleName) { // "pop" as from stack, not Imperator Pop ;)
		if (!historyDict.TryGetValue(titleName, out var historyToReturn)) {
			return null;
		}
		historyDict.Remove(titleName);
		return historyToReturn;
	}

	private void RegisterKeys() {
		RegisterRegex(@"(e|k|d|c|b)_[A-Za-z0-9_\-\']+", (reader, titleName) => {
			var historyItem = new StringOfItem(reader).ToString();
			if (!historyItem.Contains('{')) {
				return;
			}
			var tempReader = new BufferedReader(historyItem);
			if (historyDict.TryGetValue(titleName, out var existingHistory)) {
				existingHistory.Update(historyFactory, tempReader);
			} else {
				var history = historyFactory.GetHistory(tempReader);
				historyDict.Add(titleName, new TitleHistory(history));
			}
		});
		RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreItem);
	}

	private readonly HistoryFactory historyFactory = new HistoryFactory.HistoryFactoryBuilder()
		.WithSimpleField("holder", new OrderedSet<string>{"holder", "holder_ignore_head_of_faith_requirement"}, null)
		.WithSimpleField("government", "government", null)
		.WithSimpleField("liege", "liege", null)
		.WithSimpleField("development_level", "change_development_level", null)
		.WithSimpleField("succession_laws", "succession_laws", new SortedSet<string>())
		.Build();
	private readonly Dictionary<string, TitleHistory> historyDict = new();
}