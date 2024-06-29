using commonItems;
using commonItems.Collections;
using commonItems.Mods;
using System.Collections.Generic;
using System.Linq;

namespace ImperatorToCK3.Imperator.Religions;

public sealed class ReligionCollection : IdObjectCollection<string, Religion> {
	public IdObjectCollection<string, Deity> Deities { get; } = [];

	private readonly Dictionary<ulong, string> holySiteIdToDeityIdDict = [];

	public ReligionCollection(ScriptValueCollection scriptValues) {
		IDictionary<string, double> parsedReligionModifiers;
		var religionParser = new Parser();
		religionParser.RegisterKeyword("modifier", reader => {
			var modifiersAssignments = reader.GetAssignments();
			parsedReligionModifiers = modifiersAssignments
				.ToDictionary(kvp => kvp.Key, kvp => scriptValues.GetValueForString(kvp.Value))
				.Where(kvp=>kvp.Value is not null)
				.ToDictionary(kvp => kvp.Key, kvp=>(double)kvp.Value!);
		});
		religionParser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreItem);

		religionsParser = new Parser();
		religionsParser.RegisterRegex(CommonRegexes.String, (reader, religionId) => {
			parsedReligionModifiers = new Dictionary<string, double>();

			religionParser.ParseStream(reader);
			AddOrReplace(new Religion(religionId, parsedReligionModifiers));
		});
		religionsParser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);

		deitiesParser = new Parser();
		deitiesParser.RegisterRegex(CommonRegexes.String, (deityReader, deityId) => {
			var deity = new Deity(deityId, deityReader, scriptValues);
			Deities.AddOrReplace(deity);
		});
	}

	public void LoadReligions(ModFilesystem imperatorModFS) {
		Logger.Info("Loading Imperator religions...");
		religionsParser.ParseGameFolder("common/religions", imperatorModFS, "txt", recursive: true);

		Logger.IncrementProgress();
	}

	public void LoadDeities(ModFilesystem imperatorModFS) {
		Logger.Info("Loading Imperator deities...");
		deitiesParser.ParseGameFolder("common/deities", imperatorModFS, "txt", recursive: true);

		Logger.IncrementProgress();
	}

	public void LoadHolySiteDatabase(BufferedReader deityManagerReader) {
		Logger.Info("Loading Imperator holy site database...");

		var parser = new Parser();
		parser.RegisterKeyword("deities_database", databaseReader => {
			var databaseParser = new Parser();
			databaseParser.RegisterRegex(CommonRegexes.Integer, (reader, holySiteIdStr) => {
				var holySiteId = ulong.Parse(holySiteIdStr);
				var assignmentsDict = reader.GetAssignments()
					.GroupBy(a => a.Key)
					.ToDictionary(g => g.Key, g => g.Last().Value);
				if (assignmentsDict.TryGetValue("deity", out var deityIdWithQuotes)) {
					holySiteIdToDeityIdDict[holySiteId] = deityIdWithQuotes.RemQuotes();
				} else {
					Logger.Warn($"Holy site {holySiteId} has no deity!");
				}
			});
			databaseParser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreItem);
			databaseParser.ParseStream(databaseReader);
		});
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreItem);

		parser.ParseStream(deityManagerReader);

		Logger.IncrementProgress();
	}

	private string? GetDeityIdForHolySiteId(ulong holySiteId) {
		return holySiteIdToDeityIdDict.TryGetValue(holySiteId, out var deityId) ? deityId : null;
	}
	public Deity? GetDeityForHolySiteId(ulong holySiteId) {
		var deityId = GetDeityIdForHolySiteId(holySiteId);
		if (deityId is null) {
			return null;
		}
		return Deities.TryGetValue(deityId, out var deity) ? deity : null;
	}

	private readonly Parser religionsParser;
	private readonly Parser deitiesParser;
}