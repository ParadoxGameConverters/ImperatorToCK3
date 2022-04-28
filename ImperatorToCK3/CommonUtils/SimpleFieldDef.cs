using System.Collections.Generic;

namespace ImperatorToCK3.CommonUtils {
	public class SimpleFieldDef {
		public string FieldName { get; set; } = "";
		public ISet<string> Setters { get; set; } = new HashSet<string>();
		public object? InitialValue { get; set; }
	}
}