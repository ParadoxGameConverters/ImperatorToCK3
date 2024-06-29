using System;

namespace ImperatorToCK3.CK3.Characters;

public readonly struct DNAColorGeneValue : IEquatable<DNAColorGeneValue> {
	public required byte X { get; init; }
	public required byte Y { get; init; }
	public required byte XRecessive { get; init; }
	public required byte YRecessive { get; init; }

	public override string ToString() {
		return $"{X} {Y} {XRecessive} {YRecessive}";
	}

	public bool Equals(DNAColorGeneValue other) {
		return X == other.X && Y == other.Y && XRecessive == other.XRecessive && YRecessive == other.YRecessive;
	}

	public override bool Equals(object? obj) {
		return obj is DNAColorGeneValue other && Equals(other);
	}

	public override int GetHashCode() {
		return HashCode.Combine(X, Y, XRecessive, YRecessive);
	}

	public static bool operator ==(DNAColorGeneValue left, DNAColorGeneValue right) {
		return left.Equals(right);
	}

	public static bool operator !=(DNAColorGeneValue left, DNAColorGeneValue right) {
		return !(left == right);
	}
}