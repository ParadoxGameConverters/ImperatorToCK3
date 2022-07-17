using commonItems;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ImperatorToCK3.Imperator.Religions; 

public class HolySiteIdToDeityIdDictionary : IReadOnlyDictionary<ulong, string> {
	private readonly Dictionary<ulong, string> dict = new();

	public void LoadHolySiteDatabase(BufferedReader deityManagerReader) {
		Logger.Info("Loading Imperator holy site database...");
		
		var parser = new Parser();
		parser.RegisterKeyword("deities_database", databaseReader => {
			var databaseParser = new Parser();
			databaseParser.RegisterRegex(CommonRegexes.Integer, (reader, holySiteIdStr) => {
				var deityId = StringUtils.RemQuotes(reader.GetAssignments()["deity"]);
				dict[ulong.Parse(holySiteIdStr)] = deityId;
			});
			databaseParser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreItem);
			databaseParser.ParseStream(databaseReader);
		});
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreItem);
		
		parser.ParseStream(deityManagerReader);
	}

	public IEnumerator<KeyValuePair<ulong, string>> GetEnumerator() => dict.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => dict.GetEnumerator();

	public int Count => dict.Count;

	public bool ContainsKey(ulong key) => dict.ContainsKey(key);

	public bool TryGetValue(ulong key, [MaybeNullWhen(false)] out string value) => dict.TryGetValue(key, out value);

	public string this[ulong key] => dict[key];

	public IEnumerable<ulong> Keys => dict.Keys;

	public IEnumerable<string> Values => dict.Values;
}