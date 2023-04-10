using commonItems.Collections;

namespace ImperatorToCK3.CommonUtils;

public class SimpleFieldDef {
	public string FieldName { get; init; } = "";
	public OrderedSet<string> Setters { get; init; } = new();
	public object? InitialValue { get; init; }
}