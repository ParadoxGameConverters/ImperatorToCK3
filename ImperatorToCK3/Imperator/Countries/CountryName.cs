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

		// special case for revolts
		if (BaseName is null) {
			return directNameLocMatch;
		}
		var baseAdjLoc = BaseName.GetAdjectiveLocBlock(irLocDB, imperatorCountries);
		if (baseAdjLoc is null) {
			return directNameLocMatch;
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
			// special case for revolts
			var baseAdjLoc = BaseName?.GetAdjectiveLocBlock(irLocDB, imperatorCountries);
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
		if (adjective is not null) {
			return adjective;
		}
		return Name + "_ADJ";
	}

	private string ReplaceDataTypes(string loc, string language, LocDB irLocDB, CountryCollection irCountries) {
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