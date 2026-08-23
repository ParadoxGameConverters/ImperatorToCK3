using commonItems;
using commonItems.Collections;
using commonItems.Localization;
using ImperatorToCK3.CommonUtils;
using System.Collections.Frozen;
using System.Collections.Generic;

namespace ImperatorToCK3.Imperator.Armies;

internal sealed class Unit : IIdentifiable<ulong> {
	public ulong Id { get; }
	public bool IsArmy { get; private set; } = true;
	public bool IsLegion { get; private set; } = false;
	public ulong CountryId { get; set; }
	public ulong? LeaderId { get; set; } // character id
	public ulong Location { get; set; } // province id
	private List<ulong> CohortIds { get; } = [];

	public LocBlock? LocalizedName { get; private set; }
	public FrozenDictionary<string, int> MenPerUnitType { get; }

	public Unit(ulong id, BufferedReader legionReader, UnitCollection unitCollection, LocDB irLocDB, ImperatorDefines defines) {
		Id = id;

		var parser = new Parser(implicitVariableHandling: false);
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

		MenPerUnitType = CalculateMenPerUnitType(CohortIds, unitCollection.Subunits, defines.CohortSize);
	}

	private static LocBlock? GetLocalizedName(BufferedReader unitNameReader, LocDB irLocDB) {
		string? name = null;
		int ordinal = 1;
		string? family = null;
		string? governorship = null;
		LocBlock? baseNameLocBlock = null;

		// parse name block
		var parser = new Parser(implicitVariableHandling: false);
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

	internal static FrozenDictionary<string, int> CalculateMenPerUnitType(IReadOnlyCollection<ulong> cohortIds, IdObjectCollection<ulong, Subunit> subunits, int cohortSize) {
		if (cohortIds.Count == 0) {
			return FrozenDictionary<string, int>.Empty;
		}

		var menPerUnitType = new Dictionary<string, float>();
		foreach (var cohortId in cohortIds) {
			if (!subunits.TryGetValue(cohortId, out var subunit)) {
				continue;
			}

			var menInCohort = cohortSize * subunit.Strength;
			if (menPerUnitType.TryGetValue(subunit.Type, out var currentMen)) {
				menPerUnitType[subunit.Type] = currentMen + menInCohort;
			} else {
				menPerUnitType[subunit.Type] = menInCohort;
			}
		}

		if (menPerUnitType.Count == 0) {
			return FrozenDictionary<string, int>.Empty;
		}

		var frozenReadyDict = new Dictionary<string, int>(menPerUnitType.Count);
		foreach (var (unitType, men) in menPerUnitType) {
			frozenReadyDict[unitType] = (int)men;
		}

		return frozenReadyDict.ToFrozenDictionary();
	}

	public static IgnoredKeywordsSet IgnoredTokens { get; } = [];
}