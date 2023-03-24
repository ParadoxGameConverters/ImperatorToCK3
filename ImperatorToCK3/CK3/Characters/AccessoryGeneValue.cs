using System;

namespace ImperatorToCK3.CK3.Characters; 

public readonly struct AccessoryGeneValue : IEquatable<AccessoryGeneValue> {
	public required string TemplateName { get; init; }
	public required byte IntSliderValue { get; init; }
	public required string TemplateRecessiveName { get; init; }
	public required byte IntSliderValueRecessive { get; init; }

	public override string ToString() {
		return $"\"{TemplateName}\" {IntSliderValue} \"{TemplateRecessiveName}\" {IntSliderValueRecessive}";
	}

	public bool Equals(AccessoryGeneValue other) {
		return TemplateName == other.TemplateName && IntSliderValue == other.IntSliderValue && TemplateRecessiveName == other.TemplateRecessiveName && IntSliderValueRecessive == other.IntSliderValueRecessive;
	}

	public override bool Equals(object? obj) {
		return obj is AccessoryGeneValue other && Equals(other);
	}

	public override int GetHashCode() {
		return HashCode.Combine(TemplateName, IntSliderValue, TemplateRecessiveName, IntSliderValueRecessive);
	}

	public static bool operator ==(AccessoryGeneValue left, AccessoryGeneValue right) {
		return left.Equals(right);
	}

	public static bool operator !=(AccessoryGeneValue left, AccessoryGeneValue right) {
		return !(left == right);
	}
}