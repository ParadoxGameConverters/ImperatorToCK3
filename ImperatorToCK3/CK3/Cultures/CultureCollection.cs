using commonItems;
using commonItems.Collections;
using commonItems.Colors;
using commonItems.Mods;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ImperatorToCK3.CK3.Cultures; 

public class CultureCollection : IdObjectCollection<string, Culture> {
	public CultureCollection(ColorFactory colorFactory, PillarCollection pillarCollection) {
		this.pillarCollection = pillarCollection;
		InitCultureDataParser(colorFactory);
	}

	private void InitCultureDataParser(ColorFactory colorFactory) {
		cultureDataParser.RegisterKeyword("INVALIDATED_BY", reader => {
			cultureData.InvalidatingCultureIds = reader.GetStrings();
		});
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
			cultureData.Heritage = pillarCollection.Heritages.First(p => p.Id == heritageId);
		});
		cultureDataParser.RegisterKeyword("traditions", reader => {
			cultureData.TraditionIds = reader.GetStrings().ToOrderedSet();
		});
		cultureDataParser.RegisterKeyword("name_list", reader => {
			var nameListId = reader.GetString();
			if (nameListCollection.TryGetValue(nameListId, out var nameList)) {
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
		if (cultureData.NameLists.Count == 0) {
			Logger.Warn($"Culture {cultureId} has no name list defined! Skipping.");
			return;
		}
		AddOrReplace(new Culture(cultureId, cultureData));
		
		// Reset culture data for the next culture.
		cultureData = new CultureData();
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
			nameListCollection.AddOrReplace(new NameList(nameListId, reader));
		});
		parser.IgnoreAndLogUnregisteredItems();
		parser.ParseGameFolder("common/culture/name_lists", ck3ModFS, "txt", recursive: true, logFilePaths: true);
	}

	private readonly IDictionary<string, string> cultureReplacements = new Dictionary<string, string>(); // replaced culture -> replacing culture
	
	private readonly PillarCollection pillarCollection;
	private readonly IdObjectCollection<string, NameList> nameListCollection = new();
	
	private CultureData cultureData = new();
	private readonly Parser cultureDataParser = new();
}