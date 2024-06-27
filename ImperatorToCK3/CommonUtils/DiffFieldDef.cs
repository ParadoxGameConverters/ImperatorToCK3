using commonItems.Collections;

namespace ImperatorToCK3.CommonUtils;

internal sealed class DiffFieldDef {
	public string FieldName { get; init; } = "";
	public OrderedSet<string> Inserters { get; init; } = new();
	public OrderedSet<string> Removers { get; init; } = new();
}