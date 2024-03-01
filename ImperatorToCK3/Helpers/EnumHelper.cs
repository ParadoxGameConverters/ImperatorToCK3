using System;

namespace ImperatorToCK3.Helpers;

public static class EnumHelper {
	public static T Min<T>(T a, T b) where T : IComparable {
		return a.CompareTo(b) <= 0 ? a : b;
	}
}