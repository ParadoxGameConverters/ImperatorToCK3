using System;

namespace ImperatorToCK3.Outputter;

internal readonly struct PartOfFileToModify(string textBefore, string textAfter, bool warnIfNotFound = true) : IEquatable<PartOfFileToModify> {
	internal readonly string TextBefore = textBefore;
	internal readonly string TextAfter = textAfter;
	internal readonly bool WarnIfNotFound = warnIfNotFound;

	public void Deconstruct(out string textBefore, out string textAfter, out bool warnIfNotFound) {
		textBefore = TextBefore;
		textAfter = TextAfter;
		warnIfNotFound = WarnIfNotFound;
	}

	public bool Equals(PartOfFileToModify other) =>
		string.Equals(TextBefore, other.TextBefore, StringComparison.Ordinal) &&
		string.Equals(TextAfter, other.TextAfter, StringComparison.Ordinal) &&
		WarnIfNotFound == other.WarnIfNotFound;

	public override bool Equals(object? obj) => obj is PartOfFileToModify other && Equals(other);

	public override int GetHashCode() {
		var hc = new HashCode();
		hc.Add(TextBefore, StringComparer.Ordinal);
		hc.Add(TextAfter, StringComparer.Ordinal);
		hc.Add(WarnIfNotFound);
		return hc.ToHashCode();
	}

	public static bool operator ==(PartOfFileToModify left, PartOfFileToModify right) => left.Equals(right);
	public static bool operator !=(PartOfFileToModify left, PartOfFileToModify right) => !left.Equals(right);
}