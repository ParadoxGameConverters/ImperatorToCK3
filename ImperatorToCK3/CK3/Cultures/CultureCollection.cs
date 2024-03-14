using commonItems;
using commonItems.Collections;
using commonItems.Colors;
using commonItems.Mods;
using Fernandezja.ColorHashSharp;
using ImperatorToCK3.CommonUtils;
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

	private readonly IDictionary<string, string> cultureReplacements = new Dictionary<string, string>(); // replaced culture -> replacing culture
	
	protected readonly PillarCollection PillarCollection;
	protected readonly IdObjectCollection<string, NameList> NameListCollection = new();
	
	private CultureData cultureData = new();
	private readonly Parser cultureDataParser = new();
	private readonly IgnoredKeywordsSet ignoredModFlags = [];
}