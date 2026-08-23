using System.Collections.Generic;

namespace ImperatorToCK3;

public static class ConverterGlobals {
	public static string PrimaryLanguage => "english";

	public static string[] SecondaryLanguages { get; } = [
		"french", "german", "korean", "russian", "simp_chinese", "spanish",
	];

	public static IEnumerable<string> SupportedLanguages {
		get {
			yield return PrimaryLanguage;
			foreach (var language in SecondaryLanguages) {
				yield return language;
			}
		}
	}
}