using commonItems;
using commonItems.Collections;
using commonItems.Mods;
using System.Collections.Generic;
using System.IO;

namespace ImperatorToCK3.CommonUtils;
public sealed class HistoryFactory {
	public sealed class HistoryFactoryBuilder {
		private readonly List<SimpleFieldDef> simpleFieldDefs = []; // fieldName, setters, initialValue
		private readonly List<SimpleFieldDef> literalFieldDefs = []; // fieldName, setters, initialValue
		private readonly List<DiffFieldDef> diffFieldDefs = []; // fieldName, inserter, remover, initialValue

		public HistoryFactoryBuilder WithSimpleField(string fieldName, string setter, object? initialValue) {
			return WithSimpleField(fieldName, [setter], initialValue);
		}
		public HistoryFactoryBuilder WithSimpleField(string fieldName, OrderedSet<string> setters, object? initialValue) {
			simpleFieldDefs.Add(new SimpleFieldDef {
				FieldName = fieldName, Setters = setters, InitialValue = initialValue
			});
			return this;
		}

		public HistoryFactoryBuilder WithLiteralField(string fieldName, string setter) {
			literalFieldDefs.Add(new SimpleFieldDef {
				FieldName = fieldName, Setters = [setter], InitialValue = null
			});
			return this;
		}

		public HistoryFactoryBuilder WithDiffField(string fieldName, string inserter, string remover) {
			return WithDiffField(fieldName, [inserter], [remover]);
		}
		public HistoryFactoryBuilder WithDiffField(string fieldName, OrderedSet<string> inserters, OrderedSet<string> removers) {
			diffFieldDefs.Add(new DiffFieldDef {
				FieldName = fieldName, Inserters = inserters, Removers = removers
			});
			return this;
		}

		public HistoryFactory Build() {
			return new HistoryFactory(simpleFieldDefs, literalFieldDefs, diffFieldDefs);
		}
	}

	private HistoryFactory(
		List<SimpleFieldDef> simpleFieldDefs,
		List<SimpleFieldDef> literalFieldDefs,
		List<DiffFieldDef> diffFieldDefs
	) {
		this.simpleFieldDefs = simpleFieldDefs;
		this.literalFieldDefs = literalFieldDefs;
		this.diffFieldDefs = diffFieldDefs;
	}

	private Parser GetParser(History history) {
		var parser = new Parser();
		foreach (var def in this.simpleFieldDefs) {
			foreach (var setter in def.Setters) {
				parser.RegisterKeyword(setter, reader => {
					// If the value is set outside of dated blocks, override the initial value.
					var itemStr = reader.GetStringOfItem().ToString();
					var value = GetValue(itemStr);

					history.Fields[def.FieldName].InitialEntries.Add(
						new KeyValuePair<string, object>(setter, value)
					);
				});
			}
		}
		foreach (var def in this.literalFieldDefs) {
			foreach (var setter in def.Setters) {
				parser.RegisterKeyword(setter, reader => {
					// If the value is set outside of dated blocks, override the initial value.
					var itemStr = reader.GetStringOfItem().ToString();
					var value = GetValue(itemStr);

					history.Fields[def.FieldName].InitialEntries.Add(
						new KeyValuePair<string, object>(setter, value)
					);
				});
			}
		}
		foreach (var def in this.diffFieldDefs) {
			foreach (var inserterKeyword in def.Inserters) {
				parser.RegisterKeyword(inserterKeyword, reader => {
					var diffField = history.Fields[def.FieldName];
					var valueToInsert = GetValue(reader.GetString());
					diffField.InitialEntries.Add(new KeyValuePair<string, object>(inserterKeyword, valueToInsert));
				});
			}

			foreach (var removerKeyword in def.Removers) {
				parser.RegisterKeyword(removerKeyword, reader => {
					var diffField = history.Fields[def.FieldName];
					var valueToRemove = GetValue(reader.GetString());
					diffField.InitialEntries.Add(new KeyValuePair<string, object>(removerKeyword, valueToRemove));
				});
			}
		}
		parser.RegisterRegex(CommonRegexes.Date, (dateBlockReader, dateString) => {
			var date = new Date(dateString);

			var dateBlockParser = new Parser();
			foreach (var field in history.Fields) {
				field.RegisterKeywords(dateBlockParser, date);
			}
			dateBlockParser.IgnoreAndStoreUnregisteredItems(history.IgnoredKeywords);
			dateBlockParser.ParseStream(dateBlockReader);
		});
		parser.IgnoreAndStoreUnregisteredItems(history.IgnoredKeywords);

		return parser;
	}

	private void InitializeHistory(History history) {
		foreach (var def in simpleFieldDefs) {
			history.Fields.TryAdd(new SimpleHistoryField(def.FieldName, def.Setters, def.InitialValue));
		}
		foreach (var def in literalFieldDefs) {
			history.Fields.TryAdd(new LiteralHistoryField(def.FieldName, def.Setters, def.InitialValue));
		}
		foreach (var def in diffFieldDefs) {
			history.Fields.TryAdd(new DiffHistoryField(def.FieldName, def.Inserters, def.Removers));
		}
	}
	public History GetHistory() {
		var history = new History();
		InitializeHistory(history);
		return history;
	}
	public History GetHistory(BufferedReader reader) {
		var history = new History();
		InitializeHistory(history);

		GetParser(history).ParseStream(reader);

		if (history.IgnoredKeywords.Count > 0) {
			Logger.Debug($"Ignored history keywords: {history.IgnoredKeywords}");
		}
		return history;
	}
	public History GetHistory(string historyPath, ModFilesystem ck3ModFS) {
		var history = new History();
		InitializeHistory(history);

		var parser = GetParser(history);
		if (File.Exists(historyPath)) {
			parser.ParseGameFile(historyPath, ck3ModFS);
		} else {
			parser.ParseGameFolder(historyPath, ck3ModFS, "txt", recursive: true);
		}

		if (history.IgnoredKeywords.Count > 0) {
			Logger.Debug($"Ignored history keywords: {history.IgnoredKeywords}");
		}
		return history;
	}

	public void UpdateHistory(History existingHistory, BufferedReader reader) {
		InitializeHistory(existingHistory);

		GetParser(existingHistory).ParseStream(reader);
	}

	public static object GetValue(string str) {
		if (int.TryParse(str, out int intValue)) {
			return intValue;
		}

		if (double.TryParse(str, out double doubleValue)) {
			return doubleValue;
		}

		if (str.TrimStart().StartsWith('{')) {
			var collectionReader = new BufferedReader(str);
			var strings = collectionReader.GetStrings();
			return strings;
		}

		return str;
	}

	private readonly List<SimpleFieldDef> simpleFieldDefs;
	private readonly List<SimpleFieldDef> literalFieldDefs;
	private readonly List<DiffFieldDef> diffFieldDefs;
}
