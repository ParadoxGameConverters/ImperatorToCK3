using commonItems;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ImperatorToCK3.CommonUtils;
public class HistoryFactory : Parser {
	public class HistoryFactoryBuilder {
		private readonly List<SimpleFieldDef> simpleFieldDefs = new(); // fieldName, setters, initialValue
		private readonly List<ContainerFieldDef> containerFieldDefs = new(); // fieldName, setter, initialValue
		private readonly List<AdditiveContainerFieldDef> containerKeyFieldDefs = new(); // fieldName, inserter, remover, initialValue

		public HistoryFactoryBuilder WithSimpleField(string fieldName, string setter, object? initialValue) {
			return WithSimpleField(fieldName, new string[] { setter }, initialValue);
		}
		public HistoryFactoryBuilder WithSimpleField(string fieldName, IEnumerable<string> setters, object? initialValue) {
			simpleFieldDefs.Add(new() { FieldName = fieldName, Setters = setters.ToHashSet(), InitialValue = initialValue });
			return this;
		}

		public HistoryFactoryBuilder WithContainerField(string fieldName, string setter, List<object> initialValue) {
			return WithContainerField(fieldName, new string[] { setter }, initialValue);
		}
		public HistoryFactoryBuilder WithContainerField(string fieldName, IEnumerable<string> setters, List<object> initialValue) {
			containerFieldDefs.Add(new() { FieldName = fieldName, Setters = setters.ToHashSet(), InitialValue = initialValue });
			return this;
		}

		public HistoryFactoryBuilder WithContainerKeyField(string fieldName, string inserter, string remover, List<object> initialValue) {
			containerKeyFieldDefs.Add(new() { FieldName = fieldName, Inserter = inserter, Remover = remover, InitialValue = initialValue });
			return this;
		}

		public HistoryFactory Build() {
			return new HistoryFactory(simpleFieldDefs, containerFieldDefs, containerKeyFieldDefs);
		}
	}

	private HistoryFactory(
		List<SimpleFieldDef> simpleFieldDefs,
		List<ContainerFieldDef> containerFieldDefs,
		List<AdditiveContainerFieldDef> containerKeyFieldDefs
	) {
		this.simpleFieldDefs = simpleFieldDefs;
		this.containerFieldDefs = containerFieldDefs;
		this.additiveContainerFieldDefs = containerKeyFieldDefs;

		foreach (var def in this.simpleFieldDefs) {
			foreach (var setter in def.Setters) {
				RegisterKeyword(setter, reader => {
					// if the value is set outside of dated blocks, override the initial value
					history.Fields[def.FieldName].InitialValue = new FieldValue(GetValue(reader.GetString()), setter);
				});
			}
		}
		foreach (var def in this.containerFieldDefs) {
			foreach (var setter in def.Setters) {
				RegisterKeyword(setter, reader => {
					// if the value is set outside of dated blocks, override the initial value
					var strings = reader.GetStrings();
					var values = new List<object>(strings.Select(GetValue));

					history.Fields[def.FieldName].InitialValue.Value = values;
				});
			}
		}
		foreach (var def in this.additiveContainerFieldDefs) {
			//history.Fields[def.FieldName] // TODO: FINISH
			RegisterKeyword(def.Inserter, reader => {
				var value = reader.GetString();
				//history.Fields[def.FieldName].InitialValue // TODO: FINISH
			});
			RegisterKeyword(def.Remover, reader => {
				throw new NotImplementedException();
			});
		}
		RegisterRegex(CommonRegexes.Date, (reader, dateString) => {
			var date = new Date(dateString);
			var datedHistoryBlock = new DatedHistoryBlock(this.simpleFieldDefs, this.containerFieldDefs, reader);
			foreach (var (fieldName, valuesList) in datedHistoryBlock.SimpleFieldContents) {
				var lastFieldValueInBlock = valuesList.Last();
				history.Fields[fieldName].AddValueToHistory(lastFieldValueInBlock.Value, lastFieldValueInBlock.Setter, date);
			}
			foreach (var (fieldName, valuesList) in datedHistoryBlock.ContainerFieldContents) {
				var lastFieldValueInBlock = valuesList.Last();
				history.Fields[fieldName].AddValueToHistory(lastFieldValueInBlock.Value, lastFieldValueInBlock.Setter, date);
			}
		});
		RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
	}

	public History GetHistory(BufferedReader reader) {
		history = new History();
		foreach (var def in simpleFieldDefs) {
			history.Fields[def.FieldName] = new HistoryField(def.Setters, def.InitialValue);
		}
		foreach (var def in containerFieldDefs) {
			history.Fields[def.FieldName] = new HistoryField(def.Setters, def.InitialValue);
		}
		ParseStream(reader);
		return history;
	}
	public History GetHistory(string historyPath, string gamePath) {
		history = new History();
		foreach (var def in simpleFieldDefs) {
			history.Fields[def.FieldName] = new HistoryField(def.Setters, def.InitialValue);
		}
		foreach (var def in containerFieldDefs) {
			history.Fields[def.FieldName] = new HistoryField(def.Setters, def.InitialValue);
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
	private readonly List<AdditiveContainerFieldDef> additiveContainerFieldDefs;
	private History history = new();
}
