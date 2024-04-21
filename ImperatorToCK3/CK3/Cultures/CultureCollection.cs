using commonItems;
using commonItems.Collections;
using commonItems.Colors;
using commonItems.Localization;
using commonItems.Mods;
using Fernandezja.ColorHashSharp;
using ImperatorToCK3.CommonUtils;
using ImperatorToCK3.Imperator.Countries;
using ImperatorToCK3.Imperator.Inventions;
using ImperatorToCK3.Mappers.Culture;
using ImperatorToCK3.Mappers.Province;
using ImperatorToCK3.Mappers.Technology;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ImperatorToCK3.CK3.Cultures; 

public class CultureCollection : IdObjectCollection<string, Culture> {
	public CultureCollection(ColorFactory colorFactory, PillarCollection pillarCollection, ICollection<string> ck3ModFlags) {
		this.PillarCollection = pillarCollection;
		InitCultureDataParser(colorFactory, ck3ModFlags);
	}

	private void InitCultureDataParser(ColorFactory colorFactory, ICollection<string> ck3ModFlags) {
		cultureDataParser.RegisterKeyword("INVALIDATED_BY", reader => LoadInvalidatingCultureIds(ck3ModFlags, reader));
		cultureDataParser.RegisterKeyword("color", reader => {
			try {
				cultureData.Color = colorFactory.GetColor(reader);
			} catch (Exception e) {
				Logger.Warn($"Found invalid color when parsing culture! {e.Message}");
			}
		});
		cultureDataParser.RegisterKeyword("parents", reader => {
			cultureData.ParentCultureIds = reader.GetStrings().ToOrderedSet();
		});
		cultureDataParser.RegisterKeyword("heritage", reader => {
			var heritageId = reader.GetString();
			cultureData.Heritage = PillarCollection.GetHeritageForId(heritageId);
			if (cultureData.Heritage is null) {
				Logger.Warn($"Found unrecognized heritage when parsing cultures: {heritageId}");
			}
		});
		cultureDataParser.RegisterKeyword("language", reader => {
			var languageId = reader.GetString();
			cultureData.Language = PillarCollection.GetLanguageForId(languageId);
			if (cultureData.Language is null) {
				Logger.Warn($"Found unrecognized language when parsing cultures: {languageId}");
			}
		});
		cultureDataParser.RegisterKeyword("traditions", reader => {
			cultureData.TraditionIds = reader.GetStrings().ToOrderedSet();
		});
		cultureDataParser.RegisterKeyword("name_list", reader => {
			var nameListId = reader.GetString();
			if (NameListCollection.TryGetValue(nameListId, out var nameList)) {
				cultureData.NameLists.Add(nameList);
			} else {
				Logger.Warn($"Found unrecognized name list when parsing culture: {nameListId}");
			}
		});
		cultureDataParser.RegisterRegex(CommonRegexes.String, (reader, keyword) => {
			cultureData.Attributes.Add(new KeyValuePair<string, StringOfItem>(keyword, reader.GetStringOfItem()));
		});
		cultureDataParser.IgnoreAndLogUnregisteredItems();
	}
	
	private void LoadInvalidatingCultureIds(ICollection<string> ck3ModFlags, BufferedReader reader) {
		var cultureIdsPerModFlagParser = new Parser();
		
		if (ck3ModFlags.Count == 0) {
			cultureIdsPerModFlagParser.RegisterKeyword("vanilla", modCultureIdsReader => {
				cultureData.InvalidatingCultureIds = modCultureIdsReader.GetStrings();
			});
		} else {
			foreach (var modFlag in ck3ModFlags) {
				cultureIdsPerModFlagParser.RegisterKeyword(modFlag, modCultureIdsReader => {
					cultureData.InvalidatingCultureIds = modCultureIdsReader.GetStrings();
				});
			}
		}
		
		// Ignore culture IDs from mods that haven't been selected.
		cultureIdsPerModFlagParser.IgnoreAndStoreUnregisteredItems(ignoredModFlags);
		cultureIdsPerModFlagParser.ParseStream(reader);
	}
	
	public void LoadCultures(ModFilesystem ck3ModFS) {
		var parser = new Parser();
		parser.RegisterRegex(CommonRegexes.String, (reader, cultureId) => LoadCulture(cultureId, reader));
		parser.IgnoreAndLogUnregisteredItems();
		parser.ParseGameFolder("common/culture/cultures", ck3ModFS, "txt", true, logFilePaths: true);
		
		ReplaceInvalidatedParents();
	}
	
	public void LoadConverterCultures(string converterCulturesPath) {
		var parser = new Parser();
		parser.RegisterRegex(CommonRegexes.String, (reader, cultureId) => LoadCulture(cultureId, reader));
		parser.IgnoreAndLogUnregisteredItems();
		parser.ParseFile(converterCulturesPath);
		
		ReplaceInvalidatedParents();
	}

	private void LoadCulture(string cultureId, BufferedReader cultureReader) {
		cultureData = new CultureData();
		
		cultureDataParser.ParseStream(cultureReader);

		if (cultureData.InvalidatingCultureIds.Any()) {
			foreach (var existingCulture in this) {
				if (!cultureData.InvalidatingCultureIds.Contains(existingCulture.Id)) {
					continue;
				}
				Logger.Debug($"Culture {cultureId} is invalidated by existing {existingCulture.Id}.");
				cultureReplacements[cultureId] = existingCulture.Id;
				return;
			}
			Logger.Debug($"Loading optional culture {cultureId}...");
		}
		if (cultureData.Heritage is null) {
			Logger.Warn($"Culture {cultureId} has no heritage defined! Skipping.");
			return;
		}
		if (cultureData.Language is null) {
			Logger.Warn($"Culture {cultureId} has no language defined! Skipping.");
			return;
		}
		if (cultureData.NameLists.Count == 0) {
			Logger.Warn($"Culture {cultureId} has no name list defined! Skipping.");
			return;
		}
		if (cultureData.Color is null) {
			Logger.Warn($"Culture {cultureId} has no color defined! Will use generated color.");
			var color = new ColorHash().Rgb(cultureId);
			cultureData.Color = new Color(color.R, color.G, color.B);
		}
		AddOrReplace(new Culture(cultureId, cultureData));
	}

	private void ReplaceInvalidatedParents() {
		// Replace invalidated cultures in parent culture lists.
		foreach (var culture in this) {
			culture.ParentCultureIds = culture.ParentCultureIds
				.Select(id => cultureReplacements.TryGetValue(id, out var replacementId) ? replacementId : id)
				.ToOrderedSet();
		}
	}

	public void LoadNameLists(ModFilesystem ck3ModFS) {
		var parser = new Parser();
		parser.RegisterRegex(CommonRegexes.String, (reader, nameListId) => {
			NameListCollection.AddOrReplace(new NameList(nameListId, reader));
		});
		parser.IgnoreAndLogUnregisteredItems();
		parser.ParseGameFolder("common/culture/name_lists", ck3ModFS, "txt", recursive: true, logFilePaths: true);
	}

	private string? GetCK3CultureIdForImperatorCountry(Country country, CultureMapper cultureMapper, ProvinceMapper provinceMapper) {
		var irCulture = country.PrimaryCulture ?? country.Monarch?.Culture;
		if (irCulture is null) {
			if (country.CountryType == CountryType.real) {
				Logger.Warn($"Failed to get primary or monarch culture for Imperator country {country.Tag}!");
			}
			
			return null;
		}

		ulong? irProvinceId = country.CapitalProvinceId ?? country.Monarch?.ProvinceId;
		ulong? ck3ProvinceId = null;
		if (irProvinceId.HasValue) {
			ck3ProvinceId = provinceMapper.GetCK3ProvinceNumbers(irProvinceId.Value).FirstOrDefault();
		}
		
		return cultureMapper.Match(irCulture, ck3ProvinceId, irProvinceId, country.HistoricalTag);
	}

	public void ImportTechnology(CountryCollection countries, CultureMapper cultureMapper, ProvinceMapper provinceMapper, InventionsDB inventionsDB, LocDB irLocDB) { // TODO: add tests for this
		Logger.Info("Converting Imperator inventions to CK3 innovations...");
		
		var innovationMapper = new InnovationMapper();
		innovationMapper.LoadLinksAndBonuses("configurables/inventions_to_innovations_map.txt");
		innovationMapper.LogUnmappedInventions(inventionsDB, irLocDB);
		
		// Group I:R countries by corresponding CK3 culture.
		var countriesByCulture = countries.Select(c => new {
				Country = c, CK3CultureId = GetCK3CultureIdForImperatorCountry(c, cultureMapper, provinceMapper),
			})
			.Where(c => c.CK3CultureId is not null)
			.GroupBy(c => c.CK3CultureId);
		
		foreach (var grouping in countriesByCulture) {
			if (!TryGetValue(grouping.Key!, out var culture)) {
				Logger.Warn($"Can't import technology for culture {grouping.Key}: culture not found in CK3 cultures!");
				continue;
			}

			var irInventions = grouping
				.SelectMany(c => c.Country.GetActiveInventionIds(inventionsDB))
				.ToHashSet();
			culture.ImportInnovationsFromImperator(irInventions, innovationMapper);
		}
	}

	private readonly IDictionary<string, string> cultureReplacements = new Dictionary<string, string>(); // replaced culture -> replacing culture
	
	protected readonly PillarCollection PillarCollection;
	protected readonly IdObjectCollection<string, NameList> NameListCollection = new();
	
	private CultureData cultureData = new();
	private readonly Parser cultureDataParser = new();
	private readonly IgnoredKeywordsSet ignoredModFlags = [];
}