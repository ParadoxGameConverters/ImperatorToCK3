using System.Collections.Generic;
using System;
using commonItems;
using ImperatorToCK3.Mappers.Localizaton;

namespace ImperatorToCK3.Imperator.Countries {
	public class CountryName : ICloneable {
		public string Name { get; private set; } = "";
		private string? adjective;
		public CountryName? BaseName { get; private set; }

		public object Clone() {
			var clone = new CountryName {
				Name = Name,
				adjective = adjective,
				BaseName = BaseName
			};
			return clone;
		}

		public LocBlock? GetNameLocBlock(LocalizationMapper localizationMapper, Dictionary<ulong, Country?> imperatorCountries) {
			var directNameLocMatch = localizationMapper.GetLocBlockForKey(Name);
			if (directNameLocMatch is not null && Name == "CIVILWAR_FACTION_NAME") {
				// special case for revolts
				if (BaseName is not null) {
					var baseAdjLoc = BaseName.GetAdjectiveLocBlock(localizationMapper, imperatorCountries);
					if (baseAdjLoc is not null) {
						directNameLocMatch.ModifyForEveryLanguage(baseAdjLoc, (ref string orig, string modifying) => {
							orig = orig.Replace("$ADJ$", modifying);
						});
						return directNameLocMatch;
					}
				}
			}
			return directNameLocMatch;
		}
		public LocBlock? GetAdjectiveLocBlock(LocalizationMapper localizationMapper, Dictionary<ulong, Country?> imperatorCountries) {
			var adj = GetAdjective();
			var directAdjLocMatch = localizationMapper.GetLocBlockForKey(adj);
			if (directAdjLocMatch is not null && adj == "CIVILWAR_FACTION_ADJECTIVE") {
				// special case for revolts
				if (BaseName is not null) {
					var baseAdjLoc = BaseName.GetAdjectiveLocBlock(localizationMapper, imperatorCountries);
					if (baseAdjLoc is not null) {
						directAdjLocMatch.ModifyForEveryLanguage(baseAdjLoc, (ref string orig, string modifying) => {
							orig = orig.Replace("$ADJ$", modifying);
						});
						return directAdjLocMatch;
					}
				}
			} else {
				foreach(var country in imperatorCountries.Values) {
					if (country.Name == Name) {
						var countryAdjective = country.GetCountryName().GetAdjective();
						var adjLoc = localizationMapper.GetLocBlockForKey(countryAdjective);
						if (adjLoc is not null) {
							return adjLoc;
						}
					}
				}
			}

			if (!string.IsNullOrEmpty(Name)) { // as fallback, use country name (which is apparently what Imperator does)
				var adjLocalizationMatch = localizationMapper.GetLocBlockForKey(Name);
				if (adjLocalizationMatch is not null) {
					return adjLocalizationMatch;
				}
			}
			return directAdjLocMatch;
		}
		public string GetAdjective() {
			if (adjective is not null) {
				return adjective;
			}
			return Name + "_ADJ";
		}

		private static class CountryNameFactory {
			private static Parser parser = new();
			private static CountryName countryName = new();
			static CountryNameFactory() {
				parser.RegisterKeyword("name", reader => {
					countryName.Name = new SingleString(reader).String;
				});
				parser.RegisterKeyword("adjective", reader => {
					countryName.adjective = new SingleString(reader).String;
				});
				parser.RegisterKeyword("base", reader => {
					var tempCountryName = (CountryName)countryName.Clone();
					tempCountryName.BaseName = Parse(reader);
					countryName = (CountryName)tempCountryName.Clone();
				});
				parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
			}

			public static CountryName Parse(BufferedReader reader) {
				countryName = new CountryName();
				parser.ParseStream(reader);
				return countryName;
			}
		}
		public CountryName Parse(BufferedReader reader) {
			return CountryNameFactory.Parse(reader);
		}
	}
}
