namespace ImperatorToCK3.UnitTests.TestHelpers;

internal static class TextTestUtils {
	/// <summary>
	/// Normalizes newlines to LF so text assertions behave the same on Windows and *nix.
	/// </summary>
	public static string NormalizeNewlines(string text) => text.Replace("\r\n", "\n").Replace("\r", "\n");
}
