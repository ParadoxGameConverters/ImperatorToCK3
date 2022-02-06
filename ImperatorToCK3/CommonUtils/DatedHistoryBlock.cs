using commonItems;
using System.Collections.Generic;
using System.Linq;

namespace ImperatorToCK3.CommonUtils {
	public class DatedHistoryBlock : Parser {
		public Dictionary<string, List<object>> SimpleFieldContents { get; } = new();
		public Dictionary<string, List<List<object>>> ContainerFieldContents { get; } = new();

		public DatedHistoryBlock(IEnumerable<SimpleFieldDef> simpleFieldStructs, IEnumerable<ContainerFieldDef> containerFieldStructs, BufferedReader reader) {
			foreach (var fieldStruct in simpleFieldStructs) {
				RegisterKeyword(fieldStruct.Setter, reader => {
					if (!SimpleFieldContents.ContainsKey(fieldStruct.FieldName)) {
						SimpleFieldContents.Add(fieldStruct.FieldName, new());
					}
					SimpleFieldContents[fieldStruct.FieldName].Add(HistoryFactory.GetValue(reader.GetString()));
				});
			}
			foreach (var fieldStruct in containerFieldStructs) {
				RegisterKeyword(fieldStruct.Setter, reader => {
					if (!ContainerFieldContents.ContainsKey(fieldStruct.FieldName)) {
						ContainerFieldContents.Add(fieldStruct.FieldName, new());
					}

					var strings = reader.GetStrings();
					var values = new List<object>(strings.Select(HistoryFactory.GetValue));
					ContainerFieldContents[fieldStruct.FieldName].Add(values);
				});
			}
			RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
			ParseStream(reader);
			ClearRegisteredRules();
		}
	}
}
