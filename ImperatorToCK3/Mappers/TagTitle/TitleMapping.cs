using commonItems;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.Imperator.Jobs;
using ImperatorToCK3.Imperator.Provinces;
using ImperatorToCK3.Mappers.Province;
using Open.Collections;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace ImperatorToCK3.Mappers.TagTitle;

public sealed class TitleMapping {
	public string? RankMatch(string irTagOrRegion, TitleRank rank, TitleRank maxTitleRank) {
		if (imperatorTagOrRegion != irTagOrRegion) {
			return null;
		}
		if (maxTitleRank < CK3TitleRank) {
			return null;
		}
		if (ranks.Count > 0 && !ranks.Contains(rank)) {
			return null;
		}
		return ck3TitleId;
	}

	public string? GovernorshipMatch(TitleRank rank, Title.LandedTitles landedTitles, Governorship governorship, ProvinceMapper provMapper, ProvinceCollection irProvinces) {
		if (imperatorTagOrRegion != governorship.Region.Id) {
			return null;
		}
		if (ranks.Count > 0 && !ranks.Contains(rank)) {
			return null;
		}

		// If title is a de jure duchy, check if the governorship controls at least 60% of the duchy's CK3 provinces.
		if (CK3TitleRank == TitleRank.duchy) {
			var deJureDuchies = landedTitles.GetDeJureDuchies().ToImmutableHashSet();
			var duchy = deJureDuchies.FirstOrDefault(d => d.Id == ck3TitleId);
			if (duchy is null) {
				// Duchy is not de jure.
				return ck3TitleId;
			}

			var ck3ProvincesInDuchy = duchy.GetDeJureVassalsAndBelow("c").Values
				.SelectMany(c => c.CountyProvinceIds)
				.ToImmutableHashSet();

			var governorshipProvincesInDuchy = governorship.GetCK3ProvinceIds(irProvinces, provMapper)
				.Intersect(ck3ProvincesInDuchy);

			var percentage = (double)governorshipProvincesInDuchy.Count() / ck3ProvincesInDuchy.Count;
			if (percentage < 0.6) {
				Logger.Debug($"Ignoring mapping from {governorship.Country.Tag} {imperatorTagOrRegion} to {ck3TitleId} because governorship controls only {percentage:P} of the duchy's CK3 provinces.");
				return null;
			}
		}

		return ck3TitleId;
	}

	private string ck3TitleId = string.Empty;
	private string imperatorTagOrRegion = string.Empty;
	private readonly SortedSet<TitleRank> ranks = [];

	private TitleRank CK3TitleRank => Title.GetRankForId(ck3TitleId);

	private static readonly Parser parser = new();
	private static TitleMapping mappingToReturn = new();
	static TitleMapping() {
		parser.RegisterKeyword("ck3", reader => mappingToReturn.ck3TitleId = reader.GetString());
		parser.RegisterKeyword("ir", reader => mappingToReturn.imperatorTagOrRegion = reader.GetString());
		parser.RegisterKeyword("rank", reader => {
			var ranksToAdd = reader.GetString().ToCharArray().Select(TitleRankUtils.CharToTitleRank);
			mappingToReturn.ranks.AddRange(ranksToAdd);
		});
		parser.IgnoreAndLogUnregisteredItems();
	}
	public static TitleMapping Parse(BufferedReader reader) {
		mappingToReturn = new TitleMapping();
		parser.ParseStream(reader);
		return mappingToReturn;
	}
}