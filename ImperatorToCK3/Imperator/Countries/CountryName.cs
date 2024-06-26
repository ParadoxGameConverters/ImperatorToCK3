using commonItems;
using commonItems.Localization;
using System;
using System.Linq;

namespace ImperatorToCK3.Imperator.Countries;

public sealed class CountryName : ICloneable {
	public string Name { get; private set; } = "";
	private string? adjective;
	public CountryName? BaseName { get; private set; }

	public object Clone() {
		return new CountryName {
			Name = Name,
			adjective = adjective,
			BaseName = BaseName
		};
	}

	public LocBlock? GetNameLocBlock(LocDB locDB, CountryCollection imperatorCountries) {
		// If the name contains a space, it can be a composite name like "egyptian PROV4791_persia"
		// (egyptian and PROV4791_persia are both loc keys, so the resulting in-game name is Memphite Hormirzad).
		// In this case, we want to get the loc for each of them and combine them into one.
		var nameParts = Name.Split(' ');
		if (nameParts.Length > 1) {
			return GetCompositeNameLocBlock(nameParts, locDB);
		}

		var directNameLocMatch = locDB.GetLocBlockForKey(Name);
		if (directNameLocMatch is null || Name != "CIVILWAR_FACTION_NAME") {
			return directNameLocMatch;
		}

		// special case for revolts
		if (BaseName is null) {
			return directNameLocMatch;
		}
		var baseAdjLoc = BaseName.GetAdjectiveLocBlock(locDB, imperatorCountries);
		if (baseAdjLoc is null) {
			return directNameLocMatch;
		}
		var locBlockToReturn = new LocBlock(Name, directNameLocMatch);
		locBlockToReturn.ModifyForEveryLanguage(baseAdjLoc,
			(orig, modifying, language) => orig?.Replace("$ADJ$", modifying)
		);
		return locBlockToReturn;
	}

	private LocBlock GetCompositeNameLocBlock(string[] nameParts, LocDB locDB) {
		var compositeLocBlock = new LocBlock(Name, ConverterGlobals.PrimaryLanguage);
		var secondaryLanguages = ConverterGlobals.SecondaryLanguages
			.Where(l => nameParts.Any(part => locDB.GetLocBlockForKey(part)?.HasLocForLanguage(l) ?? false));
		foreach (var language in secondaryLanguages) {
			compositeLocBlock[language] = string.Empty;
		}
		foreach (var namePart in nameParts) {
			var namePartLoc = locDB.GetLocBlockForKey(namePart);
			if (namePartLoc is null) {
				continue;
			}
			
			compositeLocBlock.ModifyForEveryLanguage(namePartLoc, (orig, modifying, language) => {
				if (orig is null) {
					return modifying;
				}
				return $"{orig} {modifying}".Trim();
			});
		}

		return compositeLocBlock;
	}

	public LocBlock? GetAdjectiveLocBlock(LocDB locDB, CountryCollection imperatorCountries) {
		var adjKey = GetAdjectiveLocKey();
		var directAdjLocMatch = locDB.GetLocBlockForKey(adjKey);
		if (directAdjLocMatch is not null && adjKey == "CIVILWAR_FACTION_ADJECTIVE") {
			// special case for revolts
			var baseAdjLoc = BaseName?.GetAdjectiveLocBlock(locDB, imperatorCountries);
			if (baseAdjLoc is not null) {
				var locBlockToReturn = new LocBlock(adjKey, directAdjLocMatch);
				locBlockToReturn.ModifyForEveryLanguage(baseAdjLoc, (orig, modifying, language) =>
					orig?.Replace("$ADJ$", modifying)
				);
				return locBlockToReturn;
			}
		} else if (directAdjLocMatch is not null) {
			return directAdjLocMatch;
		}
		
		foreach (var country in imperatorCountries) {
			if (country.Name != Name) {
				continue;
			}

			var countryAdjectiveLocKey = country.CountryName.GetAdjectiveLocKey();
			var adjLoc = locDB.GetLocBlockForKey(countryAdjectiveLocKey);
			if (adjLoc is not null) {
				return adjLoc;
			}
		}
		
		// Give up.
		return null;
	}
	public string GetAdjectiveLocKey() {
		if (adjective is not null) {
			return adjective;
		}
		return Name + "_ADJ";
	}
	
	public static CountryName Parse(BufferedReader reader) {
		var countryName = new CountryName();
			
		var parser = new Parser();
		parser.RegisterKeyword("name", r => countryName.Name = r.GetString());
		parser.RegisterKeyword("adjective", r => countryName.adjective = r.GetString());
		parser.RegisterKeyword("base", r => {
			var tempCountryName = (CountryName)countryName.Clone();
			tempCountryName.BaseName = Parse(r);
			countryName = (CountryName)tempCountryName.Clone();
		});
		parser.IgnoreAndLogUnregisteredItems();
		parser.ParseStream(reader);
		return countryName;
	}
}