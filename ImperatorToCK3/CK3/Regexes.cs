using System.Text.RegularExpressions;

namespace ImperatorToCK3.CK3;

public static partial class Regexes {
	public static Regex TitleId => TitleIdRegex();

	[GeneratedRegex("^(e|k|d|c|b)_[A-Za-z0-9_\\-\\']+$")]
	private static partial Regex TitleIdRegex();
}