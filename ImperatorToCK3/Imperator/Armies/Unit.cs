using commonItems;
using commonItems.Collections;
using commonItems.Localization;
using System.Collections.Generic;
using System.Linq;

namespace ImperatorToCK3.Imperator.Armies;

public class Unit : IIdentifiable<ulong> {
	public ulong Id { get; }
	public bool IsArmy { get; private set; } = true;
	public bool IsLegion { get; private set; } = false;
	public ulong CountryId { get; set; }
	public ulong LeaderId { get; set; } // character id, TODO: convert this
	public ulong Location { get; set; } // province id
	private List<ulong> CohortIds { get; } = new();
	
	public string NameLocKey { get; }
	public LocBlock? LocalizedName;
	public IDictionary<string, int> MenPerUnitType { get; }

	public Unit(ulong id, BufferedReader legionReader, UnitCollection unitCollection, LocDB locDB, Defines defines) {
		Id = id;
		NameLocKey = $"IRToCK3_unit_{Id}";

		var parser = new Parser();
		parser.RegisterKeyword("unit_name", reader => LocalizedName = GetLocalizedName(reader, locDB));
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

	private static LocBlock? GetLocalizedName(BufferedReader unitNameReader, LocDB locDB) {
		string? name = null;
		int ordinal = 1;
		LocBlock? baseNameLocBlock = null;
		
		// parse name block
		var parser = new Parser();
		parser.RegisterKeyword("name", reader => name = reader.GetString());
		parser.RegisterKeyword("ordinal", reader => ordinal = reader.GetInt());
		parser.RegisterKeyword("base", reader => baseNameLocBlock = GetLocalizedName(reader, locDB));
		parser.IgnoreAndLogUnregisteredItems();
		parser.ParseStream(unitNameReader);
		
		// generate localized name for each language
		if (name is null) {
			return null;
		}
		var nameLocBlock = locDB.GetLocBlockForKey(name);
		if (nameLocBlock is null) {
			return null;
		}
		if (baseNameLocBlock is not null) {
			nameLocBlock.ModifyForEveryLanguage(baseNameLocBlock, (loc, baseLoc, language) => loc?.Replace("$BASE$", baseLoc));	
		}
		nameLocBlock.ModifyForEveryLanguage((loc, language) => loc?.Replace("$NUM$", ordinal.ToString()));
		nameLocBlock.ModifyForEveryLanguage((loc, language) => loc?.Replace("$ORDER$", ordinal.ToOrdinalSuffix(language)));

		return nameLocBlock;
	}
	
	private IDictionary<string, int> GetMenPerUnitType(UnitCollection unitCollection, Defines defines) {
		var cohortSize = defines.CohortSize;
		
		return unitCollection.Subunits.Where(s => CohortIds.Contains(s.Id))
			.GroupBy(s=>s.Type)
			.ToDictionary(g => g.Key, g => (int)g.Sum(s => cohortSize * s.Strength));
	}
	
	public static HashSet<string> IgnoredTokens { get; } = new();
}