using commonItems;
using commonItems.Localization;
using System;

namespace ImperatorToCK3.Imperator.Countries; 

public class CountryName : ICloneable {
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
		directNameLocMatch.ModifyForEveryLanguage(baseAdjLoc,
			(orig, modifying, language) => orig?.Replace("$ADJ$", modifying)
		);
		return directNameLocMatch;
	}
	public LocBlock? GetAdjectiveLocBlock(LocDB locDB, CountryCollection imperatorCountries) {
		var adj = GetAdjectiveLocKey();
		var directAdjLocMatch = locDB.GetLocBlockForKey(adj);
		if (directAdjLocMatch is not null && adj == "CIVILWAR_FACTION_ADJECTIVE") {
			// special case for revolts
			var baseAdjLoc = BaseName?.GetAdjectiveLocBlock(locDB, imperatorCountries);
			if (baseAdjLoc is not null) {
				directAdjLocMatch.ModifyForEveryLanguage(baseAdjLoc, (orig, modifying, language) =>
					orig?.Replace("$ADJ$", modifying)
				);
				return directAdjLocMatch;
			}
		} else {
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
		}

		if (!string.IsNullOrEmpty(Name)) { // as fallback, use country name (which is apparently what Imperator does)
			var adjLocalizationMatch = locDB.GetLocBlockForKey(Name);
			if (adjLocalizationMatch is not null) {
				return adjLocalizationMatch;
			}
		}
		return directAdjLocMatch;
	}
	public string GetAdjectiveLocKey() {
		if (adjective is not null) {
			return adjective;
		}
		return Name + "_ADJ";
	}

	private static class CountryNameFactory {
		private static readonly Parser parser = new();
		private static CountryName countryName = new();
		static CountryNameFactory() {
			parser.RegisterKeyword("name", reader =>
				countryName.Name = reader.GetString()
			);
			parser.RegisterKeyword("adjective", reader =>
				countryName.adjective = reader.GetString()
			);
			parser.RegisterKeyword("base", reader => {
				var tempCountryName = (CountryName)countryName.Clone();
				tempCountryName.BaseName = Parse(reader);
				countryName = (CountryName)tempCountryName.Clone();
			});
			parser.IgnoreAndLogUnregisteredItems();
		}

		public static CountryName Parse(BufferedReader reader) {
			countryName = new CountryName();
			parser.ParseStream(reader);
			return countryName;
		}
	}
	public static CountryName Parse(BufferedReader reader) {
		return CountryNameFactory.Parse(reader);
	}
}