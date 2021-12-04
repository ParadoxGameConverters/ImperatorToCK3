using commonItems;
using System.Collections.Generic;
using System.Linq;

namespace ImperatorToCK3.CommonUtils {
	public class HistoryFactory : Parser {
		public class HistoryFactoryBuilder {
			private readonly List<SimpleFieldDef> simpleFieldDefs = new(); // fieldName, setter, initialValue
			private readonly List<ContainerFieldDef> containerFieldDefs = new(); // fieldName, setter, initialValue

			public HistoryFactoryBuilder WithSimpleField(string fieldName, string setter, string? initialValue) {
				simpleFieldDefs.Add(new() { FieldName = fieldName, Setter = setter, InitialValue = initialValue });
				return this;
			}
			public HistoryFactoryBuilder WithContainerField(string fieldName, string setter, List<string> initialValue) {
				containerFieldDefs.Add(new() { FieldName = fieldName, Setter = setter, InitialValue = initialValue });
				return this;
			}

			public HistoryFactory Build() {
				return new HistoryFactory(simpleFieldDefs, containerFieldDefs);
			}
		}

		private HistoryFactory(List<SimpleFieldDef> simpleFieldDefs, List<ContainerFieldDef> containerFieldDefs) {
			this.simpleFieldDefs = simpleFieldDefs;
			this.containerFieldDefs = containerFieldDefs;

			foreach (var def in this.simpleFieldDefs) {
				RegisterKeyword(def.Setter, reader => {
					// if the value is set outside of dated blocks, override the initial value
					history.Fields[def.FieldName].InitialValue = reader.GetString();
				});
			}
			foreach (var def in this.containerFieldDefs) {
				RegisterKeyword(def.Setter, reader => {
					// if the value is set outside of dated blocks, override the initial value
					history.Fields[def.FieldName].InitialValue = reader.GetStrings();
				});
			}
			RegisterRegex(CommonRegexes.Date, (reader, dateString) => {
				var date = new Date(dateString);
				var contents = new DatedHistoryBlock(this.simpleFieldDefs, this.containerFieldDefs, reader).Contents;
				foreach (var (fieldName, valuesList) in contents.SimpleFieldContents) {
					history.Fields[fieldName].AddValueToHistory(valuesList.Last(), date);
				}
				foreach (var (fieldName, valuesList) in contents.ContainerFieldContents) {
					history.Fields[fieldName].AddValueToHistory(valuesList.Last(), date);
				}
			});
			RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreItem);
		}
		public History GetHistory(BufferedReader reader) {
			history = new History();
			foreach (var def in simpleFieldDefs) {
				history.Fields[def.FieldName] = new HistoryField(def.Setter, def.InitialValue);
			}
			foreach (var def in containerFieldDefs) {
				history.Fields[def.FieldName] = new HistoryField(def.Setter, def.InitialValue);
			}
			ParseStream(reader);
			return history;
		}
		public void UpdateHistory(History existingHistory, BufferedReader reader) {
			history = existingHistory;
			ParseStream(reader);
		}

		private readonly List<SimpleFieldDef> simpleFieldDefs; // fieldName, setter, initialValue
		private readonly List<ContainerFieldDef> containerFieldDefs; // fieldName, setter, initialValue
		private History history = new();
	}
}
