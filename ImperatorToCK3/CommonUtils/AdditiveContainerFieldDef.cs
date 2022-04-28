using System.Collections.Generic;

namespace ImperatorToCK3.CommonUtils;

internal class AdditiveContainerFieldDef {
	public string FieldName { get; set; } = "";
	public string Inserter { get; set; } = "";
	public string Remover { get; set; } = "";
	public List<object> InitialValue { get; set; } = new();
}