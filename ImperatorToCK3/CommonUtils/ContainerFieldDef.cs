using System.Collections.Generic;

namespace ImperatorToCK3.CommonUtils {
	public class ContainerFieldDef {
		public string FieldName { get; set; } = "";
		public string Setter { get; set; } = "";
		public List<string> InitialValue { get; set; } = new();
	}
}