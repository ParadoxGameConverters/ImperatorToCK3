using commonItems;

namespace ImperatorToCK3.CommonUtils;

public sealed class FieldValue {
	public object? Value { get; set; }
	public string Setter { get; set; } // setter that was used to set the value
	public FieldValue(object? value, string setter) {
		Value = value;
		Setter = setter;
	}

	public void Add(string valueToAdd) {
		if (Value is commonItems.Collections.OrderedSet<string> additiveCollection) {
			additiveCollection.Add(valueToAdd);
		} else {
			Logger.Warn($"Cannot additively add value to {Value}!");
		}
	}
	public void Remove(string valueToRemove) {
		if (Value is commonItems.Collections.OrderedSet<string> additiveCollection) {
			additiveCollection.Remove(valueToRemove);
		} else {
			Logger.Warn($"Cannot additively remove value from {Value}!");
		}
	}
}
