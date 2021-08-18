using System.Collections.Generic;
using commonItems;

namespace ImperatorToCK3.CommonUtils {
	public class DatedHistoryBlock : Parser {
		public ContentsClass Contents { get; } = new();

		public DatedHistoryBlock(List<SimpleFieldDef> simpleFieldStructs, List<ContainerFieldDef> containerFieldStructs, BufferedReader reader) {
			foreach (var fieldStruct in simpleFieldStructs) {
				RegisterKeyword(fieldStruct.Setter, (reader) => {
					if (!Contents.simpleFieldContents.ContainsKey(fieldStruct.FieldName)) {
						Contents.simpleFieldContents.Add(fieldStruct.FieldName, new());
					}
					Contents.simpleFieldContents[fieldStruct.FieldName].Add(new SingleString(reader).String);
				});
			}
			foreach (var fieldStruct in containerFieldStructs) {
				RegisterKeyword(fieldStruct.Setter, (reader) => {
					if (!Contents.containerFieldContents.ContainsKey(fieldStruct.FieldName)) {
						Contents.containerFieldContents.Add(fieldStruct.FieldName, new());
					}
					Contents.containerFieldContents[fieldStruct.FieldName].Add(new StringList(reader).Strings);
				});
			}
			RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreItem);
			ParseStream(reader);
			ClearRegisteredRules();
		}
	}
}
