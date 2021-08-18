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
				RegisterKeyword(fieldStruct.setter, (reader) => {
					// if the value is set outside of dated blocks, override the initial value
					history.simpleFields[fieldStruct.fieldName].InitialValue = new SingleString(reader).String;
				});
			}
			foreach (var fieldStruct in this.containerFieldStructs) {
				RegisterKeyword(fieldStruct.setter, (reader) => {
					// if the value is set outside of dated blocks, override the initial value
					history.containerFields[fieldStruct.fieldName].InitialValue = new StringList(reader).Strings;
				});
			}
			RegisterRegex(CommonRegexes.Date, (reader, dateString) => {
				var date = new Date(dateString);
				var contents = new DatedHistoryBlock(this.simpleFieldStructs, this.containerFieldStructs, reader).Contents;
				foreach (var (fieldName, valuesList) in contents.simpleFieldContents) {
					history.simpleFields[fieldName].AddValueToHistory(valuesList.Last(), date);
				}
				foreach (var (fieldName, valuesList) in contents.containerFieldContents) {
					history.containerFields[fieldName].AddValueToHistory(valuesList.Last(), date);
				}
			});
			RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreItem);
		}
		public History GetHistory(BufferedReader reader) {
			history = new History();
			foreach (var fieldStruct in simpleFieldStructs) {
				history.simpleFields[fieldStruct.fieldName] = new SimpleField(fieldStruct.initialValue);
			}
			foreach (var fieldStruct in containerFieldStructs) {
				history.containerFields[fieldStruct.fieldName] = new ContainerField(fieldStruct.initialValue);
			}
			ParseStream(reader);
			return history;
		}
	}
}
