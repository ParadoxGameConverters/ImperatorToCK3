using commonItems;
using System.Collections.Generic;
using System.Linq;

namespace ImperatorToCK3.CommonUtils;

public class DatedHistoryBlock : Parser {
	public Dictionary<string, List<FieldValue>> SimpleFieldContents { get; } = new();
	public Dictionary<string, List<FieldValue>> ContainerFieldContents { get; } = new();

	public DatedHistoryBlock(IEnumerable<SimpleFieldDef> simpleFieldStructs, IEnumerable<ContainerFieldDef> containerFieldStructs, BufferedReader reader) {
		foreach (var fieldStruct in simpleFieldStructs) {
			foreach (var setter in fieldStruct.Setters) {
				RegisterKeyword(setter, reader => {
					if (!SimpleFieldContents.ContainsKey(fieldStruct.FieldName)) {
						SimpleFieldContents.Add(fieldStruct.FieldName, new());
					}
					SimpleFieldContents[fieldStruct.FieldName].Add(new FieldValue(HistoryFactory.GetValue(reader.GetString()), setter));
				});
			}
		}
		foreach (var fieldStruct in containerFieldStructs) {
			foreach (var setter in fieldStruct.Setters) {
				RegisterKeyword(setter, reader => {
					if (!ContainerFieldContents.ContainsKey(fieldStruct.FieldName)) {
						ContainerFieldContents.Add(fieldStruct.FieldName, new());
					}

					var strings = reader.GetStrings();
					var values = new List<object>(strings.Select(HistoryFactory.GetValue));
					ContainerFieldContents[fieldStruct.FieldName].Add(new FieldValue(values, setter));
				});
			}
		}
		RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		ParseStream(reader);
		ClearRegisteredRules();
	}
}
