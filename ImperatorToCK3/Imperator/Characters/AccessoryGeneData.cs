using System;

namespace ImperatorToCK3.Imperator.Characters;

public readonly struct AccessoryGeneData : IEquatable<AccessoryGeneData> {
	public required string GeneTemplate { get; init; }
	public required string ObjectName { get; init; }
	public required string GeneTemplateRecessive { get; init; }
	public required string ObjectNameRecessive { get; init; }

	public bool Equals(AccessoryGeneData other) {
		return GeneTemplate == other.GeneTemplate && ObjectName == other.ObjectName && GeneTemplateRecessive == other.GeneTemplateRecessive && ObjectNameRecessive == other.ObjectNameRecessive;
	}

	public override bool Equals(object? obj) {
		return obj is AccessoryGeneData other && Equals(other);
	}

	public override int GetHashCode() {
		return HashCode.Combine(GeneTemplate, ObjectName, GeneTemplateRecessive, ObjectNameRecessive);
	}

	public static bool operator ==(AccessoryGeneData left, AccessoryGeneData right) {
		return left.Equals(right);
	}

	public static bool operator !=(AccessoryGeneData left, AccessoryGeneData right) {
		return !(left == right);
	}
}