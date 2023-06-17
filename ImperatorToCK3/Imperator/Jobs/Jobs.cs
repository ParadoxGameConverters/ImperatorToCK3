using commonItems;
using ImperatorToCK3.CommonUtils;
using ImperatorToCK3.Imperator.Countries;
using ImperatorToCK3.Mappers.Region;
using System.Collections.Generic;

namespace ImperatorToCK3.Imperator.Jobs;

public class Jobs {
	public List<Governorship> Governorships { get; } = new();

	public Jobs() { }
	public Jobs(BufferedReader jobsReader, CountryCollection countries, ImperatorRegionMapper irRegionMapper) {
		var ignoredTokens = new IgnoredKeywordsSet();
		var parser = new Parser();
		parser.RegisterKeyword("province_job", reader => {
			Governorships.Add(new Governorship(reader, countries, irRegionMapper));
		});
		parser.IgnoreAndStoreUnregisteredItems(ignoredTokens);

		parser.ParseStream(jobsReader);
		Logger.Debug($"Ignored Jobs tokens: {ignoredTokens}");
	}
}