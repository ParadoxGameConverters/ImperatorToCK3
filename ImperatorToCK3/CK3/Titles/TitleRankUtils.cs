using System;

namespace ImperatorToCK3.CK3.Titles;

public static class TitleRankUtils {
	public static TitleRank CharToTitleRank(char rankChar) {
		return rankChar switch {
			'b' => TitleRank.barony,
			'c' => TitleRank.county,
			'd' => TitleRank.duchy,
			'k' => TitleRank.kingdom,
			'e' => TitleRank.empire,
			'h' => TitleRank.hegemony,
			_ => throw new ArgumentOutOfRangeException(nameof(rankChar), $"Unknown title rank character: {rankChar}")
		};
	}
}