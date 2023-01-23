using System.Text.RegularExpressions;

namespace ImperatorToCK3.CK3;

public static class Regexes {
	public static Regex TitleId => new(@"^(e|k|d|c|b)_[A-Za-z0-9_\-\']+$");
}