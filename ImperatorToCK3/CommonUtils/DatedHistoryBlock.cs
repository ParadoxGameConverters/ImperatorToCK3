using System.Collections.Generic;
using commonItems;

namespace ImperatorToCK3.CommonUtils {
	public class ContentsClass
	{
		public Dictionary<string, List<string>> simpleFieldContents = new();
		public Dictionary<string, List<List<string>>> containerFieldContents = new();
	}

	public class DatedHistoryBlock : Parser
	{
		public ContentsClass Contents { get; } = new();

		public DatedHistoryBlock(List<SimpleFieldDef> simpleFieldStructs, List<ContainerFieldDef> containerFieldStructs, BufferedReader reader) {
			foreach (var fieldStruct in simpleFieldStructs) {
				RegisterKeyword(fieldStruct.setter, (reader) => {
					if (!Contents.simpleFieldContents.ContainsKey(fieldStruct.fieldName)) {
						Contents.simpleFieldContents.Add(fieldStruct.fieldName, new());
					}
					Contents.simpleFieldContents[fieldStruct.fieldName].Add(new SingleString(reader).String);
				});
			}
			foreach (var fieldStruct in containerFieldStructs) {
				RegisterKeyword(fieldStruct.setter, (reader) => {
					if (!Contents.containerFieldContents.ContainsKey(fieldStruct.fieldName)) {
						Contents.containerFieldContents.Add(fieldStruct.fieldName, new());
					}
					Contents.containerFieldContents[fieldStruct.fieldName].Add(new StringList(reader).Strings);
				});
			}
			RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreItem);
			ParseStream(reader);
			ClearRegisteredRules();
		}
	}
}
