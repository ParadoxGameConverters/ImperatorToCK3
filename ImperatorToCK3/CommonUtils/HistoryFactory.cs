using commonItems;
using commonItems.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ImperatorToCK3.CommonUtils;
public sealed class HistoryFactory : Parser {
	public class HistoryFactoryBuilder {
		private readonly List<SimpleFieldDef> simpleFieldDefs = new(); // fieldName, setters, initialValue
		private readonly List<ContainerFieldDef> containerFieldDefs = new(); // fieldName, setter, initialValue
		private readonly List<AdditiveContainerFieldDef> containerKeyFieldDefs = new(); // fieldName, inserter, remover, initialValue

		public HistoryFactoryBuilder WithSimpleField(string fieldName, string setter, object? initialValue) {
			return WithSimpleField(fieldName, new OrderedSet<string> { setter }, initialValue);
		}
		public HistoryFactoryBuilder WithSimpleField(string fieldName, OrderedSet<string> setters, object? initialValue) {
			simpleFieldDefs.Add(new() { FieldName = fieldName, Setters = setters, InitialValue = initialValue });
			return this;
		}

		public HistoryFactoryBuilder WithContainerField(string fieldName, string setter, List<object> initialValue) {
			return WithContainerField(fieldName, new OrderedSet<string> { setter }, initialValue);
		}
		public HistoryFactoryBuilder WithContainerField(string fieldName, OrderedSet<string> setters, List<object> initialValue) {
			containerFieldDefs.Add(new() { FieldName = fieldName, Setters = setters, InitialValue = initialValue });
			return this;
		}

		public HistoryFactoryBuilder WithAdditiveContainerField(string fieldName, string inserter, string remover) {
			containerKeyFieldDefs.Add(new() { FieldName = fieldName, Inserter = inserter, Remover = remover });
			return this;
		}

		public HistoryFactory Build() {
			return new HistoryFactory(simpleFieldDefs, containerFieldDefs, containerKeyFieldDefs);
		}
	}

	private HistoryFactory(
		List<SimpleFieldDef> simpleFieldDefs,
		List<ContainerFieldDef> containerFieldDefs,
		List<AdditiveContainerFieldDef> diffFieldDefs
	) {
		this.simpleFieldDefs = simpleFieldDefs;
		this.containerFieldDefs = containerFieldDefs;
		this.diffFieldDefs = diffFieldDefs;

		foreach (var def in this.simpleFieldDefs) {
			foreach (var setter in def.Setters) {
				RegisterKeyword(setter, reader => {
					// if the value is set outside of dated blocks, override the initial value
					history.Fields[def.FieldName].InitialEntries.Add(
						new KeyValuePair<string, object>(setter, GetValue(reader.GetString()))
					);
				});
			}
		}
		foreach (var def in this.containerFieldDefs) {
			foreach (var setter in def.Setters) {
				RegisterKeyword(setter, reader => {
					// if the value is set outside of dated blocks, override the initial value
					var strings = reader.GetStrings();
					var values = new List<object>(strings.Select(GetValue));

					history.Fields[def.FieldName].InitialEntries.Add(
						new KeyValuePair<string, object>(setter, values)
					);
				});
			}
		}
		foreach (var def in this.diffFieldDefs) {
			RegisterKeyword(def.Inserter, reader => {
				var diffField = (DiffHistoryField)history.Fields[def.FieldName];
				var valueToInsert = GetValue(reader.GetString());
				diffField.InitialEntries.Add(new KeyValuePair<string, object>(def.Inserter, valueToInsert));
			});
			RegisterKeyword(def.Remover, reader => {
				var diffField = (DiffHistoryField)history.Fields[def.FieldName];
				var valueToRemove = GetValue(reader.GetString());
				diffField.InitialEntries.Add(new KeyValuePair<string, object>(def.Remover, valueToRemove));
			});
		}
		RegisterRegex(CommonRegexes.Date, (reader, dateString) => {
			var date = new Date(dateString);
			
			var dateBlockParser = new Parser();
			foreach (var field in history.Fields.Values) {
				field.RegisterKeywords(dateBlockParser, date);
			}
			dateBlockParser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
			dateBlockParser.ParseStream(reader);
		});
		RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
	}

	public History GetHistory(BufferedReader reader) {
		history = new History();
		foreach (var def in simpleFieldDefs) {
			history.Fields[def.FieldName] = new SimpleHistoryField(def.FieldName, def.Setters, def.InitialValue);
		}
		foreach (var def in containerFieldDefs) {
			history.Fields[def.FieldName] = new SimpleHistoryField(def.FieldName, def.Setters, def.InitialValue);
		}
		foreach (var def in diffFieldDefs) {
			history.Fields[def.FieldName] = new DiffHistoryField(def.FieldName, new OrderedSet<string> {def.Inserter}, new OrderedSet<string> {def.Remover});
		}
		ParseStream(reader);
		return history;
	}
	public History GetHistory(string historyPath, string gamePath) {
		history = new History();
		foreach (var def in simpleFieldDefs) {
			history.Fields[def.FieldName] = new SimpleHistoryField(def.FieldName, def.Setters, def.InitialValue);
		}
		foreach (var def in containerFieldDefs) {
			history.Fields[def.FieldName] = new SimpleHistoryField(def.FieldName, def.Setters, def.InitialValue);
		}
		foreach (var def in diffFieldDefs) {
			history.Fields[def.FieldName] = new DiffHistoryField(def.FieldName, new OrderedSet<string> {def.Inserter}, new OrderedSet<string> {def.Remover});
		}

		if (File.Exists(historyPath)) {
			ParseGameFile(historyPath, gamePath, new List<Mod>());
		} else {
			ParseGameFolder(historyPath, gamePath, "txt", new List<Mod>(), true);
		}
		return history;
	}

	public void UpdateHistory(History existingHistory, BufferedReader reader) {
		history = existingHistory;
		ParseStream(reader);
	}

	public static object GetValue(string str) {
		if (int.TryParse(str, out int intValue)) {
			return intValue;
		}

		if (double.TryParse(str, out double doubleValue)) {
			return doubleValue;
		}

		return str;
	}

	private readonly List<SimpleFieldDef> simpleFieldDefs;
	private readonly List<ContainerFieldDef> containerFieldDefs;
	private readonly List<AdditiveContainerFieldDef> diffFieldDefs;
	private History history = new();
}
