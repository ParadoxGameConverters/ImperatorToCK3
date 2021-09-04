using System.Collections.Generic;
using System.Linq;
using commonItems;

namespace ImperatorToCK3.CommonUtils {
	public class HistoryFactory : Parser {
		private History history = new();
		private readonly List<SimpleFieldDef> simpleFieldDefs; // fieldName, setter, initialValue
		private readonly List<ContainerFieldDef> containerFieldDefs; // fieldName, setter, initialValue

		public HistoryFactory(List<SimpleFieldDef> simpleFieldDefs, List<ContainerFieldDef> containerFieldDefs) {
			this.simpleFieldDefs = simpleFieldDefs;
			this.containerFieldDefs = containerFieldDefs;

			foreach (var def in this.simpleFieldDefs) {
				RegisterKeyword(def.Setter, (reader) => {
					// if the value is set outside of dated blocks, override the initial value
					history.SimpleFields[def.FieldName].InitialValue = new SingleString(reader).String;
				});
			}
			foreach (var def in this.containerFieldDefs) {
				RegisterKeyword(def.Setter, (reader) => {
					// if the value is set outside of dated blocks, override the initial value
					history.ContainerFields[def.FieldName].InitialValue = new StringList(reader).Strings;
				});
			}
			RegisterRegex(CommonRegexes.Date, (reader, dateString) => {
				var date = new Date(dateString);
				var contents = new DatedHistoryBlock(this.simpleFieldDefs, this.containerFieldDefs, reader).Contents;
				foreach (var (fieldName, valuesList) in contents.SimpleFieldContents) {
					history.SimpleFields[fieldName].AddValueToHistory(valuesList.Last(), date);
				}
				foreach (var (fieldName, valuesList) in contents.ContainerFieldContents) {
					history.ContainerFields[fieldName].AddValueToHistory(valuesList.Last(), date);
				}
			});
			RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreItem);
		}
		public History GetHistory(BufferedReader reader) {
			history = new History();
			foreach (var def in simpleFieldDefs) {
				history.SimpleFields[def.FieldName] = new SimpleField(def.InitialValue);
			}
			foreach (var def in containerFieldDefs) {
				history.ContainerFields[def.FieldName] = new ContainerField(def.InitialValue);
			}
			ParseStream(reader);
			return history;
		}
		public void UpdateHistory(History existingHistory, BufferedReader reader) {
			history = existingHistory;
			ParseStream(reader);
		}
	}
}
