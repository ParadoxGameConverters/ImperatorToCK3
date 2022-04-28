using commonItems.Collections;
using System.Collections.Generic;

namespace ImperatorToCK3.CommonUtils {
	public class SimpleFieldDef {
		public string FieldName { get; set; } = "";
		public OrderedSet<string> Setters { get; set; } = new();
		public object? InitialValue { get; set; }
	}
}