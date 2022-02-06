using System.Collections.Generic;

namespace ImperatorToCK3.CommonUtils;

internal class ContainerKeyFieldDef {
	public string FieldName { get; set; } = "";
	public string Inserter { get; set; } = "";
	public string Remover { get; set; } = "";
	public List<string> InitialValue { get; set; } = new();
}