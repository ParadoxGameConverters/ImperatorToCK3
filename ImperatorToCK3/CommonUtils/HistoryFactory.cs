using System.Collections.Generic;
using System.Linq;
using commonItems;

namespace ImperatorToCK3.CommonUtils {
	public class HistoryFactory : Parser {
		private History history = new();
		private readonly List<SimpleFieldDef> simpleFieldStructs; // fieldName, setter, initialValue
		private readonly List<ContainerFieldDef> containerFieldStructs; // fieldName, setter, initialValue

		public HistoryFactory(List<SimpleFieldDef> simpleFieldStructs, List<ContainerFieldDef> containerFieldStructs) {
			this.simpleFieldStructs = simpleFieldStructs;
			this.containerFieldStructs = containerFieldStructs;

			foreach (var fieldStruct in this.simpleFieldStructs) {
				RegisterKeyword(fieldStruct.Setter, (reader) => {
					// if the value is set outside of dated blocks, override the initial value
					history.SimpleFields[fieldStruct.FieldName].InitialValue = new SingleString(reader).String;
				});
			}
			foreach (var fieldStruct in this.containerFieldStructs) {
				RegisterKeyword(fieldStruct.Setter, (reader) => {
					// if the value is set outside of dated blocks, override the initial value
					history.ContainerFields[fieldStruct.FieldName].InitialValue = new StringList(reader).Strings;
				});
			}
			RegisterRegex(CommonRegexes.Date, (reader, dateString) => {
				var date = new Date(dateString);
				var contents = new DatedHistoryBlock(this.simpleFieldStructs, this.containerFieldStructs, reader).Contents;
				foreach (var (fieldName, valuesList) in contents.simpleFieldContents) {
					history.SimpleFields[fieldName].AddValueToHistory(valuesList.Last(), date);
				}
				foreach (var (fieldName, valuesList) in contents.containerFieldContents) {
					history.ContainerFields[fieldName].AddValueToHistory(valuesList.Last(), date);
				}
			});
			RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreItem);
		}
		public History GetHistory(BufferedReader reader) {
			history = new History();
			foreach (var fieldStruct in simpleFieldStructs) {
				history.SimpleFields[fieldStruct.FieldName] = new SimpleField(fieldStruct.InitialValue);
			}
			foreach (var fieldStruct in containerFieldStructs) {
				history.ContainerFields[fieldStruct.FieldName] = new ContainerField(fieldStruct.InitialValue);
			}
			ParseStream(reader);
			return history;
		}
	}
}
