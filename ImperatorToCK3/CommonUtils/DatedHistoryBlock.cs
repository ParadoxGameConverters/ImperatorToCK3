using commonItems;
using System.Collections.Generic;

namespace ImperatorToCK3.CommonUtils {
	public class DatedHistoryBlock : Parser {
		public ContentsClass Contents { get; } = new();

		public DatedHistoryBlock(IEnumerable<SimpleFieldDef> simpleFieldStructs, IEnumerable<ContainerFieldDef> containerFieldStructs, BufferedReader reader) {
			foreach (var fieldStruct in simpleFieldStructs) {
				RegisterKeyword(fieldStruct.Setter, reader => {
					if (!Contents.SimpleFieldContents.ContainsKey(fieldStruct.FieldName)) {
						Contents.SimpleFieldContents.Add(fieldStruct.FieldName, new());
					}
					Contents.SimpleFieldContents[fieldStruct.FieldName].Add(reader.GetString());
				});
			}
			foreach (var fieldStruct in containerFieldStructs) {
				RegisterKeyword(fieldStruct.Setter, reader => {
					if (!Contents.ContainerFieldContents.ContainsKey(fieldStruct.FieldName)) {
						Contents.ContainerFieldContents.Add(fieldStruct.FieldName, new());
					}
					Contents.ContainerFieldContents[fieldStruct.FieldName].Add(reader.GetStrings());
				});
			}
			RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreItem);
			ParseStream(reader);
			ClearRegisteredRules();
		}
	}
}
