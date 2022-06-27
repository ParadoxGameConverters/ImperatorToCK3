using commonItems.Collections;
using System.Collections.Generic;

namespace ImperatorToCK3.Imperator.Religions; 

public sealed class Religion : IIdentifiable<string> {
	public string Id { get; }
	public IDictionary<string, float> Modifiers { get; }

	public Religion(string id, IDictionary<string, float> modifiers) {
		Id = id;
		Modifiers = new Dictionary<string, float>(modifiers);
	}
}