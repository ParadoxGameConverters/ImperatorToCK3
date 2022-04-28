using System.Collections.Generic;

namespace ImperatorToCK3.CommonUtils;

public class ContainerFieldDef {
	public string FieldName { get; set; } = "";
	public ISet<string> Setters { get; set; } = new HashSet<string>();
	public List<object> InitialValue { get; set; } = new();
}