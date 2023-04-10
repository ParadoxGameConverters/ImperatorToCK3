using System;

namespace ImperatorToCK3.Imperator.Characters; 

public readonly struct MorphGeneData : IEquatable<MorphGeneData> {
	public required string TemplateName { get; init; }
	public required byte Value { get; init; }
	public required string TemplateRecessiveName { get; init; }
	public required byte ValueRecessive { get; init; }

	public bool Equals(MorphGeneData other) {
		return TemplateName == other.TemplateName && Value == other.Value && TemplateRecessiveName == other.TemplateRecessiveName && ValueRecessive == other.ValueRecessive;
	}

	public override bool Equals(object? obj) {
		return obj is MorphGeneData other && Equals(other);
	}

	public override int GetHashCode() {
		return HashCode.Combine(TemplateName, Value, TemplateRecessiveName, ValueRecessive);
	}

	public static bool operator ==(MorphGeneData left, MorphGeneData right) {
		return left.Equals(right);
	}

	public static bool operator !=(MorphGeneData left, MorphGeneData right) {
		return !(left == right);
	}
}