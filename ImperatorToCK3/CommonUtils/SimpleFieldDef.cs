using commonItems.Collections;

namespace ImperatorToCK3.CommonUtils;

internal sealed class SimpleFieldDef {
	public string FieldName { get; init; } = "";
	public OrderedSet<string> Setters { get; init; } = [];
	public object? InitialValue { get; init; }
}