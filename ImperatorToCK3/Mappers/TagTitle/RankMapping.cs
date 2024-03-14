using commonItems;
using ImperatorToCK3.CK3.Titles;

namespace ImperatorToCK3.Mappers.TagTitle;

public class RankMapping {
	public RankMapping(BufferedReader mappingReader) {
		var parser = new Parser();
		parser.RegisterKeyword("ir", reader => irRank = reader.GetString());
		parser.RegisterKeyword("required_territories", reader => requiredTerritories = reader.GetInt());
		parser.RegisterKeyword("ck3", reader => ck3Rank = TitleRankUtils.CharToTitleRank(reader.GetChar()));
		parser.IgnoreAndLogUnregisteredItems();
		parser.ParseStream(mappingReader);
	}

	public TitleRank? Match(string imperatorRank, int territoriesCount) {
		if (irRank is not null && imperatorRank != irRank) {
			return null;
		}
		
		if (requiredTerritories > 0 && territoriesCount < requiredTerritories) {
			return null;
		}
		
		return ck3Rank;
	}

	private string? irRank;
	private int requiredTerritories = 0;
	private TitleRank? ck3Rank;
}