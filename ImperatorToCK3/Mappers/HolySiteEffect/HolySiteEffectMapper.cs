using commonItems;
using System.Collections.Generic;

namespace ImperatorToCK3.Mappers.HolySiteEffect;

public class HolySiteEffectMapper {
	private readonly Dictionary<string, KeyValuePair<string, double>> effectMap = new(); // imperator effect, <ck3 effect, factor>

	public HolySiteEffectMapper(string mappingsFilePath) {
		var parser = new Parser();
		parser.RegisterKeyword("link", mappingReader => {
			string? imp = null;
			string? ck3 = null;
			double factor = 1;

			var mappingParser = new Parser();
			mappingParser.RegisterKeyword("imp", reader => imp = reader.GetString());
			mappingParser.RegisterKeyword("ck3", reader => ck3 = reader.GetString());
			mappingParser.RegisterKeyword("factor", reader => factor = reader.GetDouble());
			mappingParser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
			mappingParser.ParseStream(mappingReader);

			if (imp is null || ck3 is null) {
				Logger.Warn($"Holy site effect mapping {imp} {ck3} {factor} has no imp or ck3 entry!");
			} else {
				effectMap[imp] = new KeyValuePair<string, double>(ck3, factor);
			}
		});
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		parser.ParseFile(mappingsFilePath);
	}

	public KeyValuePair<string, double>? Match(string imperatorEffect, double imperatorValue) {
		if (!effectMap.TryGetValue(imperatorEffect, out var match)) {
			return null;
		}

		var (ck3Effect, factor) = match;
		var ck3Value = imperatorValue * factor;
		return new KeyValuePair<string, double>(ck3Effect, ck3Value);
	}
}