using commonItems;
using commonItems.Collections;
using commonItems.Localization;
using ImperatorToCK3.CommonUtils;
using System.Collections.Generic;
using System.Linq;

namespace ImperatorToCK3.Imperator.Armies;

public sealed class Unit : IIdentifiable<ulong> {
	public ulong Id { get; }
	public bool IsArmy { get; private set; } = true;
	public bool IsLegion { get; private set; } = false;
	public ulong CountryId { get; set; }
	public ulong LeaderId { get; set; } // character id
	public ulong Location { get; set; } // province id
	private List<ulong> CohortIds { get; } = new();

	public LocBlock? LocalizedName { get; private set; }
	public IDictionary<string, int> MenPerUnitType { get; }

	public Unit(ulong id, BufferedReader legionReader, UnitCollection unitCollection, LocDB irLocDB, Defines defines) {
		Id = id;

		var parser = new Parser();
		parser.RegisterKeyword("unit_name", reader => LocalizedName = GetLocalizedName(reader, irLocDB));
		parser.RegisterKeyword("is_army", reader => IsArmy = reader.GetBool());
		parser.RegisterKeyword("country", reader => CountryId = reader.GetULong());
		parser.RegisterKeyword("leader", reader => LeaderId = reader.GetULong());
		parser.RegisterKeyword("location", reader => Location = reader.GetULong());
		parser.RegisterKeyword("cohort", reader => CohortIds.Add(reader.GetULong()));
		parser.RegisterKeyword("legion", reader => {
			ParserHelpers.IgnoreItem(reader);
			IsLegion = true;
		});
		parser.IgnoreAndStoreUnregisteredItems(IgnoredTokens);
		parser.ParseStream(legionReader);

		MenPerUnitType = GetMenPerUnitType(unitCollection, defines);
	}

	private static LocBlock? GetLocalizedName(BufferedReader unitNameReader, LocDB irLocDB) {
		string? name = null;
		int ordinal = 1;
		string? family = null;
		string? governorship = null;
		LocBlock? baseNameLocBlock = null;

		// parse name block
		var parser = new Parser();
		parser.RegisterKeyword("name", reader => name = reader.GetString());
		parser.RegisterKeyword("ordinal", reader => ordinal = reader.GetInt());
		parser.RegisterKeyword("family", reader => family = reader.GetString());
		parser.RegisterKeyword("governorship", reader => governorship = reader.GetString());
		parser.RegisterKeyword("base", reader => baseNameLocBlock = GetLocalizedName(reader, irLocDB));
		parser.IgnoreAndLogUnregisteredItems();
		parser.ParseStream(unitNameReader);

		// generate localized name for each language
		if (name is null) {
			return null;
		}
		var rawLoc = irLocDB.GetLocBlockForKey(name);
		if (rawLoc is null) {
			return null;
		}

		var nameLocBlockId = $"IRToCK3_unit_{name}_{ordinal}";
		if (baseNameLocBlock is not null) {
			nameLocBlockId += $"_{baseNameLocBlock?.Id}";
		}
		var nameLocBlock = new LocBlock(nameLocBlockId, rawLoc);

		if (baseNameLocBlock is not null) {
			nameLocBlock.ModifyForEveryLanguage(baseNameLocBlock, (loc, baseLoc, language) => loc?.Replace("$BASE$", baseLoc));
		}
		nameLocBlock.ModifyForEveryLanguage((loc, language) => loc?.Replace("$ROMAN$", ordinal.ToRomanNumeral()));
		nameLocBlock.ModifyForEveryLanguage((loc, language) => loc?.Replace("$NUM$", ordinal.ToString()));
		nameLocBlock.ModifyForEveryLanguage((loc, language) => loc?.Replace("$ORDER$", ordinal.ToOrdinalSuffix(language)));
		nameLocBlock.ModifyForEveryLanguage((loc, language) => loc?.Replace("$FAMILY$", family));
		nameLocBlock.ModifyForEveryLanguage((loc, language) => loc?.Replace("$GOVERNORSHIP$", governorship));

		return nameLocBlock;
	}

	private Dictionary<string, int> GetMenPerUnitType(UnitCollection unitCollection, Defines defines) {
		var cohortSize = defines.CohortSize;

		return unitCollection.Subunits.Where(s => CohortIds.Contains(s.Id))
			.GroupBy(s=>s.Type)
			.ToDictionary(g => g.Key, g => (int)g.Sum(s => cohortSize * s.Strength));
	}

	public static IgnoredKeywordsSet IgnoredTokens { get; } = [];
}