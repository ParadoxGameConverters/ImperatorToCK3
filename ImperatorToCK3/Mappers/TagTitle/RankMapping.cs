using commonItems;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.Imperator.Countries;
using System;
using System.Collections.Generic;

namespace ImperatorToCK3.Mappers.TagTitle;

internal sealed class RankMapping {
	public RankMapping(BufferedReader mappingReader) {
		var parser = new Parser();
		parser.RegisterKeyword("ir", reader => irRank = reader.GetString());
		parser.RegisterKeyword("required_territories", reader => requiredTerritories = reader.GetInt());
		parser.RegisterKeyword("ir_government_type", reader => {
			var governmentType = Enum.Parse<GovernmentType>(reader.GetString(), ignoreCase: true);
			requiredIRGovernmentTypes.Add(governmentType);
		});
		parser.RegisterKeyword("ck3", reader => ck3Rank = TitleRankUtils.CharToTitleRank(reader.GetChar()));
		parser.IgnoreAndLogUnregisteredItems();
		parser.ParseStream(mappingReader);
	}

	public TitleRank? Match(string imperatorRank, int territoriesCount, GovernmentType irGovernmentType) {
		if (irRank is not null && imperatorRank != irRank) {
			return null;
		}

		if (requiredTerritories > 0 && territoriesCount < requiredTerritories) {
			return null;
		}

		if (requiredIRGovernmentTypes.Count > 0 && !requiredIRGovernmentTypes.Contains(irGovernmentType)) {
			return null;
		}

		return ck3Rank;
	}

	private string? irRank;
	private int requiredTerritories = 0;
	private readonly HashSet<GovernmentType> requiredIRGovernmentTypes = [];
	private TitleRank? ck3Rank;
}