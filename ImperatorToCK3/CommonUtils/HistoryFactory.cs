using System.Collections.Generic;
using System.Linq;
using commonItems;

namespace ImperatorToCK3.CommonUtils {
	public class HistoryFactory : Parser {
		private History history = new();
		private readonly List<SimpleFieldStruct> simpleFieldStructs; // fieldName, setter, initialValue
		private readonly List<ContainerFieldStruct> containerFieldStructs; // fieldName, setter, initialValue

		public HistoryFactory(List<SimpleFieldStruct> simpleFieldStructs, List<ContainerFieldStruct> containerFieldStructs) {
			this.simpleFieldStructs = simpleFieldStructs;
			this.containerFieldStructs = containerFieldStructs;

			//var tempSimpleFields = new Dictionary<string, SimpleField>();
			//var tempContainerFields = new Dictionary<string, ContainerField>();

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
				foreach (var (fieldName, valuecList) in contents.Item1) {
					history.simpleFields[fieldName].AddValueToHistory(valuecList.Last(), date);
				}
				foreach (var (fieldName, valuecList) in contents.Item2) {
					history.containerFields[fieldName].AddValueToHistory(valuecList.Last(), date);
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
