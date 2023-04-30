using commonItems;
using ImperatorToCK3.CommonUtils;
using System.Collections.Generic;

namespace ImperatorToCK3.Imperator.Jobs;

public class Jobs {
	public List<Governorship> Governorships { get; } = new();

	public Jobs() { }
	public Jobs(BufferedReader reader) {
		var ignoredTokens = new IgnoredKeywordsSet();
		var parser = new Parser();
		parser.RegisterKeyword("province_job", reader => {
			Governorships.Add(new Governorship(reader));
		});
		parser.IgnoreAndStoreUnregisteredItems(ignoredTokens);

		parser.ParseStream(reader);
		Logger.Debug($"Ignored Jobs tokens: {ignoredTokens}");
	}
}