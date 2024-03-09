using commonItems;
using ImperatorToCK3.CommonUtils;
using ImperatorToCK3.Imperator.Countries;
using ImperatorToCK3.Mappers.Region;
using System.Collections.Generic;

namespace ImperatorToCK3.Imperator.Jobs;

public class JobsDB {
	public IList<Governorship> Governorships { get; } = new List<Governorship>();

	public JobsDB() { }
	public JobsDB(BufferedReader jobsReader, CountryCollection countries, ImperatorRegionMapper irRegionMapper) {
		var ignoredTokens = new IgnoredKeywordsSet();
		var parser = new Parser();
		parser.RegisterKeyword("province_job", reader => {
			try {
				Governorships.Add(new Governorship(reader, countries, irRegionMapper));
			} catch (System.Exception ex) {
				Logger.Warn($"Failed to load governorship: {ex.Message}");
			}
		});
		parser.IgnoreAndStoreUnregisteredItems(ignoredTokens);

		parser.ParseStream(jobsReader);
		Logger.Debug($"Ignored Jobs tokens: {ignoredTokens}");
	}
}