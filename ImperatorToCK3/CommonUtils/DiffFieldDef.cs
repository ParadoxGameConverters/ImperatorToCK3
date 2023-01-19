using commonItems.Collections;

namespace ImperatorToCK3.CommonUtils;

internal class DiffFieldDef {
	public string FieldName { get; init; } = "";
	public OrderedSet<string> Inserters { get; set; } = new();
	public OrderedSet<string> Removers { get; set; } = new();
}