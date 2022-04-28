using commonItems.Collections;
using System.Collections.Generic;

namespace ImperatorToCK3.CommonUtils;

public class ContainerFieldDef {
	public string FieldName { get; set; } = "";
	public OrderedSet<string> Setters { get; set; } = new();
	public List<object> InitialValue { get; set; } = new();
}