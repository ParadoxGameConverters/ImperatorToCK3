﻿using commonItems;
using commonItems.Localization;
using System.Linq;

namespace ImperatorToCK3.Imperator.Countries;

internal sealed class CountryName {
	public string Name { get; init; } = "";
	private string? adjective;
	public CountryName? BaseName { get; private init; }

	public LocBlock? GetNameLocBlock(LocDB irLocDB, CountryCollection imperatorCountries) {
		// If the name contains a space, it can be a composite name like "egyptian PROV4791_persia"
		// (egyptian and PROV4791_persia are both loc keys, so the resulting in-game name is Memphite Hormirzad).
		// In this case, we want to get the loc for each of them and combine them into one.
		var nameParts = Name.Split(' ');
		if (nameParts.Length > 1) {
			return GetCompositeNameLocBlock(nameParts, irLocDB);
		}

		var directNameLocMatch = irLocDB.GetLocBlockForKey(Name);
		if (directNameLocMatch is null || Name != "CIVILWAR_FACTION_NAME") {
			return directNameLocMatch;
		}

		// Special case for revolts.
		if (BaseName is null) {
			return directNameLocMatch;
		}

		var baseAdjLoc = BaseName.GetAdjectiveLocBlock(irLocDB, imperatorCountries) ??
		                 BaseName.GetNameLocBlock(irLocDB, imperatorCountries);
		if (baseAdjLoc is null) {
			// If the base name only has an unlocalized name, use it.
			baseAdjLoc = new LocBlock(BaseName.Name, ConverterGlobals.PrimaryLanguage) {
				[ConverterGlobals.PrimaryLanguage] = BaseName.Name,
			};
			foreach (var language in ConverterGlobals.SecondaryLanguages) {
				baseAdjLoc[language] = BaseName.Name;
			}
		}

		var locBlockToReturn = new LocBlock(Name, directNameLocMatch);
		locBlockToReturn.ModifyForEveryLanguage(baseAdjLoc,
			(orig, modifying, language) => {
				string? toReturn = orig?.Replace("$ADJ$", modifying);
				if (toReturn is not null) {
					toReturn = ReplaceDataTypes(toReturn, language, irLocDB, imperatorCountries);
				}

				return toReturn;
			});
		return locBlockToReturn;
	}

	private LocBlock GetCompositeNameLocBlock(string[] nameParts, LocDB irLocDB) {
		var compositeLocBlock = new LocBlock(Name, ConverterGlobals.PrimaryLanguage);
		var secondaryLanguages = ConverterGlobals.SecondaryLanguages
			.Where(l => nameParts.Any(part => irLocDB.GetLocBlockForKey(part)?.HasLocForLanguage(l) ?? false));
		foreach (var language in secondaryLanguages) {
			compositeLocBlock[language] = string.Empty;
		}

		foreach (var namePart in nameParts) {
			var namePartLoc = irLocDB.GetLocBlockForKey(namePart);
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

	public LocBlock? GetAdjectiveLocBlock(LocDB irLocDB, CountryCollection imperatorCountries) {
		var adjKey = GetAdjectiveLocKey();
		var directAdjLocMatch = irLocDB.GetLocBlockForKey(adjKey);
		if (directAdjLocMatch is not null && adjKey == "CIVILWAR_FACTION_ADJECTIVE") {
			// Special case for revolts.
			// If the BaseName only has a name and no adjective, use the name.
			var baseAdjLoc = BaseName?.GetAdjectiveLocBlock(irLocDB, imperatorCountries) ??
			                 BaseName?.GetNameLocBlock(irLocDB, imperatorCountries);
			// If neither localized adjective nor name is found, use the unlocalized name.
			if (baseAdjLoc is null && BaseName is not null) {
				baseAdjLoc = new LocBlock(BaseName.Name, ConverterGlobals.PrimaryLanguage) {
					[ConverterGlobals.PrimaryLanguage] = BaseName.Name,
				};
				foreach (var language in ConverterGlobals.SecondaryLanguages) {
					baseAdjLoc[language] = BaseName.Name;
				}
			}

			if (baseAdjLoc is not null) {
				var locBlockToReturn = new LocBlock(adjKey, directAdjLocMatch);
				locBlockToReturn.ModifyForEveryLanguage(baseAdjLoc, (orig, modifying, language) => {
					var toReturn = orig?.Replace("$ADJ$", modifying);
					if (toReturn is not null) {
						toReturn = ReplaceDataTypes(toReturn, language, irLocDB, imperatorCountries);
					}

					return toReturn;
				});
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
			var adjLoc = irLocDB.GetLocBlockForKey(countryAdjectiveLocKey);
			if (adjLoc is not null) {
				return adjLoc;
			}
		}

		// Give up.
		return null;
	}

	public string GetAdjectiveLocKey() {
		return adjective ?? $"{Name}_ADJ";
	}

	private static string ReplaceDataTypes(string loc, string language, LocDB irLocDB, CountryCollection irCountries) {
		if (!loc.Contains("[GetCountry(")) {
			return loc;
		}

		const string phrygianAdj = "[GetCountry('PRY').Custom('get_pry_adj')]";
		Country? pry = irCountries.FirstOrDefault(country => country.Tag == "PRY");
		LocBlock? pryAdjLocBlock;
		if (pry is not null && pry.Monarch?.Family?.Key == "Antigonid") {
			pryAdjLocBlock = irLocDB.GetLocBlockForKey("get_pry_adj_fetch");
		} else {
			pryAdjLocBlock = irLocDB.GetLocBlockForKey("get_pry_adj_fallback");
		}

		if (pryAdjLocBlock is not null && pryAdjLocBlock.HasLocForLanguage(language)) {
			loc = loc.Replace(phrygianAdj, pryAdjLocBlock[language]);
		}

		const string mauryanAdj = "[GetCountry('MRY').Custom('get_mry_adj')]";
		Country? mry = irCountries.FirstOrDefault(country => country.Tag == "MRY");
		LocBlock? mryAdjLocBlock;
		if (mry is not null && mry.Monarch?.Family?.Key == "Maurya") {
			mryAdjLocBlock = irLocDB.GetLocBlockForKey("get_mry_adj_fetch");
		} else {
			mryAdjLocBlock = irLocDB.GetLocBlockForKey("get_mry_adj_fallback");
		}

		if (mryAdjLocBlock is not null && mryAdjLocBlock.HasLocForLanguage(language)) {
			loc = loc.Replace(mauryanAdj, mryAdjLocBlock[language]);
		}

		const string seleucidAdj = "[GetCountry('SEL').Custom('get_sel_adj')]";
		Country? sel = irCountries.FirstOrDefault(country => country.Tag == "SEL");
		LocBlock? selAdjLocBlock;
		if (sel is not null && sel.Monarch?.Family?.Key == "Seleukid") {
			selAdjLocBlock = irLocDB.GetLocBlockForKey("get_sel_adj_fetch");
		} else {
			selAdjLocBlock = irLocDB.GetLocBlockForKey("get_sel_adj_fallback");
		}

		if (selAdjLocBlock is not null && selAdjLocBlock.HasLocForLanguage(language)) {
			loc = loc.Replace(seleucidAdj, selAdjLocBlock[language]);
		}

		return loc;
	}

	public static CountryName Parse(BufferedReader reader) {
		string parsedName = string.Empty;
		string? parsedAdjective = null;
		CountryName? parsedBaseName = null;
		
		var parser = new Parser();
		parser.RegisterKeyword("name", r => parsedName = r.GetString());
		parser.RegisterKeyword("adjective", r => parsedAdjective = r.GetString());
		parser.RegisterKeyword("base", r => {
			parsedBaseName = Parse(r);
		});
		parser.IgnoreAndLogUnregisteredItems();
		parser.ParseStream(reader);

		return new() {Name = parsedName, adjective = parsedAdjective, BaseName = parsedBaseName};
	}
}