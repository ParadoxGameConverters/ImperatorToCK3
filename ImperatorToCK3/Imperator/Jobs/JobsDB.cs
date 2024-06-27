using commonItems;
using ImperatorToCK3.CommonUtils;
using ImperatorToCK3.Imperator.Characters;
using ImperatorToCK3.Imperator.Countries;
using ImperatorToCK3.Mappers.Region;
using System;
using System.Collections.Generic;

namespace ImperatorToCK3.Imperator.Jobs;

public sealed class JobsDB {
	public IList<Governorship> Governorships { get; } = [];
	public IList<OfficeJob> OfficeJobs { get; } = [];

	public JobsDB() { }
	public JobsDB(BufferedReader jobsReader, CharacterCollection characters, CountryCollection countries, ImperatorRegionMapper irRegionMapper) {
		var ignoredTokens = new IgnoredKeywordsSet();
		var parser = new Parser();
		parser.RegisterKeyword("province_job", reader => {
			try {
				Governorships.Add(new Governorship(reader, countries, irRegionMapper));
			} catch (Exception ex) {
				Logger.Warn($"Failed to load governorship: {ex.Message}");
			}
		});
		parser.RegisterKeyword("office_job", reader => {
			try {
				OfficeJobs.Add(new OfficeJob(reader, characters));
			} catch (Exception ex) {
				Logger.Warn($"Failed to load office job: {ex.Message}");
			}
		});
		parser.IgnoreAndStoreUnregisteredItems(ignoredTokens);

		parser.ParseStream(jobsReader);
		Logger.Debug($"Ignored Jobs tokens: {ignoredTokens}");
	}
}