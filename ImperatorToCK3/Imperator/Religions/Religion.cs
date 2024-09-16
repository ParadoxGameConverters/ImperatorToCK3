using commonItems.Collections;
using System.Collections.Generic;

namespace ImperatorToCK3.Imperator.Religions;

public sealed class Religion : IIdentifiable<string> {
	public string Id { get; }
	public OrderedDictionary<string, double> Modifiers { get; }

	public Religion(string id, OrderedDictionary<string, double> modifiers) {
		Id = id;
		Modifiers = new(modifiers);
	}
}