using commonItems;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.Imperator.Jobs;
using ImperatorToCK3.Imperator.Provinces;
using ImperatorToCK3.Mappers.Province;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace ImperatorToCK3.Mappers.TagTitle;

public class Mapping {
	public string? RankMatch(string imperatorTagOrRegion, string rank) {
		if (this.imperatorTagOrRegion != imperatorTagOrRegion) {
			return null;
		}
		if (ranks.Count > 0 && !ranks.Contains(rank)) {
			return null;
		}
		return ck3Title;
	}

	public string? GovernorshipMatch(string rank, Title.LandedTitles landedTitles, Governorship governorship, ProvinceMapper provMapper, ProvinceCollection irProvinces) {
		if (imperatorTagOrRegion != governorship.Region.Id) {
			return null;
		}
		if (ranks.Count > 0 && !ranks.Contains(rank)) {
			return null;
		}
		
		// If title is a de jure duchy, check if the governorship controls at least 60% of the duchy's CK3 provinces.
		if (ck3Title.StartsWith("d_")) {
			var deJureDuchies = landedTitles.GetDeJureDuchies().ToImmutableHashSet();
			var duchy = deJureDuchies.FirstOrDefault(d => d.Id == ck3Title);
			if (duchy is not null) {
				var ck3ProvincesInDuchy = duchy.GetDeJureVassalsAndBelow("c").Values
					.SelectMany(c => c.CountyProvinces)
					.ToImmutableHashSet();

				var governorshipProvincesInDuchy = governorship.GetCK3ProvinceIds(irProvinces, provMapper)
					.Intersect(ck3ProvincesInDuchy);
			
				var percentage = (double)governorshipProvincesInDuchy.Count() / ck3ProvincesInDuchy.Count;
				if (percentage < 0.6) {
					Logger.Debug($"Ignoring mapping from {governorship.Country.Tag} {imperatorTagOrRegion} to {ck3Title} because governorship controls only {percentage:P} of the duchy's CK3 provinces.");
					return null;
				}
			}
		}

		return ck3Title;
	}

	private string ck3Title = string.Empty;
	private string imperatorTagOrRegion = string.Empty;
	private readonly SortedSet<string> ranks = new();

	private static readonly Parser parser = new();
	private static Mapping mappingToReturn = new();
	static Mapping() {
		parser.RegisterKeyword("ck3", reader => mappingToReturn.ck3Title = reader.GetString());
		parser.RegisterKeyword("ir", reader => mappingToReturn.imperatorTagOrRegion = reader.GetString());
		parser.RegisterKeyword("rank", reader => mappingToReturn.ranks.Add(reader.GetString()));
		parser.IgnoreAndLogUnregisteredItems();
	}
	public static Mapping Parse(BufferedReader reader) {
		mappingToReturn = new Mapping();
		parser.ParseStream(reader);
		return mappingToReturn;
	}
}