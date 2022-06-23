using commonItems;
using commonItems.Collections;

namespace ImperatorToCK3.Imperator.Religions; 

public class Religion : IIdentifiable<string> {
	public string Id { get; }
	public string? ModifierId { get; set; }
	public float? ModifierValue { get; set; }

	public Religion(string id, string? modifierId, float? modifierValue) {
		Id = id;
	}
}