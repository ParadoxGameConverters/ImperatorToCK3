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

internal class CultureCollection : IdObjectCollection<string, Culture> {
	public CultureCollection(ColorFactory colorFactory, PillarCollection pillarCollection, OrderedDictionary<string, bool> ck3ModFlags) {
		this.PillarCollection = pillarCollection;
		InitCultureDataParser(colorFactory, ck3ModFlags);
	}

	private void InitCultureDataParser(ColorFactory colorFactory, OrderedDictionary<string, bool> ck3ModFlags) {
		cultureDataParser.RegisterKeyword("INVALIDATED_BY", reader => LoadInvalidatingCultureIds(ck3ModFlags, reader));
		cultureDataParser.RegisterModDependentBloc(ck3ModFlags);
		cultureDataParser.RegisterKeyword("color", reader => {
			try {
				cultureData.Color = colorFactory.GetColor(reader);
			} catch (Exception e) {
				Logger.Warn($"Found invalid color when parsing culture! {e.Message}");
			}
		});
		cultureDataParser.RegisterKeyword("parents", reader => {
			cultureData.ParentCultureIds = reader.GetStrings().ToOrderedSet();

			if (cultureData.ParentCultureIds.Count > 2) {
				Logger.Warn("Found a culture that has more than 2 parents! Only the first 2 will be used.");
				cultureData.ParentCultureIds = cultureData.ParentCultureIds.Take(2).ToOrderedSet();
			}
		});
		cultureDataParser.RegisterKeyword("heritage", reader => {
			var heritageId = reader.GetString();
			cultureData.Heritage = PillarCollection.GetHeritageForId(heritageId);
			if (cultureData.Heritage is null) {
				Logger.Debug($"Found unrecognized heritage when parsing cultures: {heritageId}");
			}
		});
		cultureDataParser.RegisterKeyword("language", reader => {
			var languageId = reader.GetString();
			cultureData.Language = PillarCollection.GetLanguageForId(languageId);
			if (cultureData.Language is null) {
				Logger.Debug($"Found unrecognized language when parsing cultures: {languageId}");
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

	private void LoadInvalidatingCultureIds(OrderedDictionary<string, bool> ck3ModFlags, BufferedReader reader) {
		var cultureIdsPerModFlagParser = new Parser();

		if (ck3ModFlags.Count == 0) {
			cultureIdsPerModFlagParser.RegisterKeyword("vanilla", modCultureIdsReader => {
				cultureData.InvalidatingCultureIds = modCultureIdsReader.GetStrings();
			});
		} else {
			foreach (var modFlag in ck3ModFlags.Where(f => f.Value)) {
				cultureIdsPerModFlagParser.RegisterKeyword(modFlag.Key, modCultureIdsReader => {
					cultureData.InvalidatingCultureIds = modCultureIdsReader.GetStrings();
				});
			}
		}

		// Ignore culture IDs from mods that haven't been selected.
		cultureIdsPerModFlagParser.IgnoreAndStoreUnregisteredItems(ignoredModFlags);
		cultureIdsPerModFlagParser.ParseStream(reader);
	}

	public void LoadCultures(ModFilesystem ck3ModFS, Configuration config) {
		Logger.Info("Loading cultures...");
		
		OrderedDictionary<string, CultureData> culturesData = new(); // Preserves order of insertion.

		var parser = new Parser();
		parser.RegisterRegex(CommonRegexes.String, (reader, cultureId) => culturesData[cultureId] = LoadCultureData(reader));
		parser.IgnoreAndLogUnregisteredItems();
		parser.ParseGameFolder("common/culture/cultures", ck3ModFS, "txt", recursive: true, logFilePaths: true);
		
		// After we've load all cultures data, we can validate it and create cultures.
		ValidateAndLoadCultures(culturesData, config);

		ReplaceInvalidatedParents();
	}

	public void LoadConverterCultures(string converterCulturesPath, Configuration config) {
		Logger.Info("Loading converter cultures...");
		
		OrderedDictionary<string, CultureData> culturesData = new(); // Preserves order of insertion.

		var parser = new Parser();
		parser.RegisterRegex(CommonRegexes.String, (reader, cultureId) => culturesData[cultureId] = LoadCultureData(reader));
		parser.IgnoreAndLogUnregisteredItems();
		parser.ParseFile(converterCulturesPath);
		
		// After we've load all cultures data, we can validate it and create cultures.
		ValidateAndLoadCultures(culturesData, config);

		ReplaceInvalidatedParents();
	}

	private CultureData LoadCultureData(BufferedReader cultureReader) {
		cultureData = new CultureData();

		cultureDataParser.ParseStream(cultureReader);
		return cultureData;
	}

	private void ValidateAndLoadCultures(OrderedDictionary<string, CultureData> culturesData, Configuration config) {
		foreach (var (cultureId, data) in culturesData) {
			if (data.InvalidatingCultureIds.Count != 0) {
				bool isInvalidated = false;
				foreach (var existingCulture in this) {
					if (!data.InvalidatingCultureIds.Contains(existingCulture.Id)) {
						continue;
					}
					Logger.Debug($"Culture {cultureId} is invalidated by existing {existingCulture.Id}.");
					cultureReplacements[cultureId] = existingCulture.Id;
					isInvalidated = true;
				}
				if (isInvalidated) {
					continue;
				}
				Logger.Debug($"Loading optional culture {cultureId}...");
			}
			if (data.Heritage is null) {
				// Special handling for TFE hunnic culture. #TODO: remove this when it's fixed on TFE side.
				if (config.FallenEagleEnabled && cultureId == "hunnic" && PillarCollection.GetHeritageForId("heritage_turkic") is Pillar turkicHeritage) {
					Logger.Debug("Applying turkic heritage to TFE hunnic culture.");
					data.Heritage = turkicHeritage;
				} else {
					Logger.Warn($"Culture {cultureId} has no valid heritage defined! Skipping.");
					continue;
				}
			}
			if (data.Language is null) {
				Logger.Warn($"Culture {cultureId} has no valid language defined! Skipping.");
				continue;
			}
			if (data.NameLists.Count == 0) {
				Logger.Warn($"Culture {cultureId} has no name list defined! Skipping.");
				continue;
			}
			if (data.Color is null) {
				Logger.Warn($"Culture {cultureId} has no color defined! Will use generated color.");
				var color = new ColorHash().Rgb(cultureId);
				data.Color = new Color(color.R, color.G, color.B);
			}
			
			AddOrReplace(new Culture(cultureId, data));
		}
	}

	private void ReplaceInvalidatedParents() {
		// Replace invalidated cultures in parent culture lists.
		foreach (var culture in this) {
			culture.ParentCultureIds = culture.ParentCultureIds
				.Select(id => cultureReplacements.GetValueOrDefault(id, defaultValue: id))
				.ToOrderedSet();
		}
	}

	public void LoadNameLists(ModFilesystem ck3ModFS) {
		Logger.Info("Loading name lists...");

		var parser = new Parser();
		parser.RegisterRegex(CommonRegexes.String, (reader, nameListId) => {
			NameListCollection.AddOrReplace(new NameList(nameListId, reader));
		});
		parser.IgnoreAndLogUnregisteredItems();
		parser.ParseGameFolder("common/culture/name_lists", ck3ModFS, "txt", recursive: true, logFilePaths: true);
	}

	public void LoadInnovationIds(ModFilesystem ck3ModFS) {
		Logger.Info("Loading CK3 innovation IDs...");

		var parser = new Parser();
		parser.RegisterRegex(CommonRegexes.String, (reader, innovationId) => {
			InnovationIds.Add(innovationId);
			ParserHelpers.IgnoreItem(reader);
		});
		parser.IgnoreAndLogUnregisteredItems();
		parser.ParseGameFolder("common/culture/innovations", ck3ModFS, "txt", recursive: true, logFilePaths: true);
	}

	private static string? GetCK3CultureIdForImperatorCountry(Country country, CultureMapper cultureMapper, ProvinceMapper provinceMapper) {
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

	public void ImportTechnology(CountryCollection countries, CultureMapper cultureMapper, ProvinceMapper provinceMapper, InventionsDB inventionsDB, LocDB irLocDB, OrderedDictionary<string, bool> ck3ModFlags) { // TODO: add tests for this
		Logger.Info("Converting Imperator inventions to CK3 innovations...");

		var innovationMapper = new InnovationMapper();
		innovationMapper.LoadLinksAndBonuses("configurables/inventions_to_innovations_map.liquid", ck3ModFlags);
		innovationMapper.LogUnmappedInventions(inventionsDB, irLocDB);
		innovationMapper.RemoveMappingsWithInvalidInnovations(InnovationIds);

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

	internal void WarnAboutCircularParents() {
		// For every culture, check if it isn't set as its own immediate or distant parent.
		Logger.Debug("Checking for circular culture parents...");
		foreach (var culture in this) {
			var allParents = GetAncestorsOfCulture(culture);
			if (allParents.Contains(culture.Id)) {
				Logger.Error($"Culture {culture.Id} is set as its own parent!");
			}
		}
	}

	private HashSet<string> GetAncestorsOfCulture(Culture cultureToCheck, HashSet<string>? alreadyChecked = null) {
		HashSet<string> allParents = [];

		// Get immediate parents.
		foreach (var parentCultureId in cultureToCheck.ParentCultureIds) {
			// Avoid infinite recursion.
			if (alreadyChecked?.Contains(parentCultureId) == true) {
				continue;
			}

			allParents.Add(parentCultureId);
			
			if (!TryGetValue(parentCultureId, out var parentCulture)) {
				Logger.Warn($"Parent culture {parentCultureId} not found for culture {cultureToCheck.Id}.");
				continue;
			}

			// Add the parent's parents.
			var parentParents = GetAncestorsOfCulture(parentCulture, allParents);
			allParents.UnionWith(parentParents);
		}

		return allParents;
	}

	private readonly Dictionary<string, string> cultureReplacements = []; // replaced culture -> replacing culture

	protected readonly PillarCollection PillarCollection;
	protected readonly IdObjectCollection<string, NameList> NameListCollection = [];
	protected readonly HashSet<string> InnovationIds = [];

	private CultureData cultureData = new();
	private readonly Parser cultureDataParser = new();
	private readonly IgnoredKeywordsSet ignoredModFlags = [];
}