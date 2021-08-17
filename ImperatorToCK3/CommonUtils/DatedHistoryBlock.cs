using System;
using System.Collections.Generic;
using commonItems;

namespace ImperatorToCK3.CommonUtils {
	public class DatedHistoryBlock : Parser {
		public Tuple<
			Dictionary<string, List<string>>, // simpleFieldContents
			Dictionary<string, List<List<string>>> // containerFieldContents
		> Contents { get; private set; } = new(new(), new());

		private List<SimpleFieldStruct> simpleFieldStructs; // fieldName, setter, defaultValue
		private List<ContainerFieldStruct> containerFieldStructs; // fieldName, setter

		public DatedHistoryBlock(List<SimpleFieldStruct> simpleFieldStructs, List<ContainerFieldStruct> containerFieldStructs, BufferedReader reader) {
			this.simpleFieldStructs = simpleFieldStructs;
			this.containerFieldStructs = containerFieldStructs;

			foreach (var fieldStruct in this.simpleFieldStructs) {
				RegisterKeyword(fieldStruct.setter, (reader) => {
					Contents.Item1[fieldStruct.fieldName].Add(new SingleString(reader).String);
				});
			}
			foreach (var fieldStruct in this.containerFieldStructs) {
				RegisterKeyword(fieldStruct.setter, (reader) => {
					Contents.Item2[fieldStruct.fieldName].Add(new StringList(reader).Strings);
				});
			}
			RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreItem);
			ParseStream(reader);
			ClearRegisteredRules();
		}
	}
}
