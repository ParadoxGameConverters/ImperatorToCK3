using commonItems.Collections;

namespace ImperatorToCK3.Imperator;

public readonly struct Variable(string id, object value) : IIdentifiable<string> {
	public string Id { get; } = id;
	public object Value { get; init; } = value;
}