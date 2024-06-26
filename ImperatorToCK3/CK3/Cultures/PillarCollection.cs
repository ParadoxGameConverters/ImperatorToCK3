using commonItems;
using commonItems.Collections;
using commonItems.Colors;
using commonItems.Mods;
using ImperatorToCK3.CommonUtils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ImperatorToCK3.CK3.Cultures; 

public sealed class PillarCollection : IdObjectCollection<string, Pillar> {
	private readonly Dictionary<string, string> mergedPillarsDict = [];

	public PillarCollection(ColorFactory colorFactory, ICollection<string> ck3ModFlags) {
		InitPillarDataParser(colorFactory, ck3ModFlags);
	}

	public Pillar? GetHeritageForId(string heritageId) {
		var heritages = this.Where(p => p.Type == "heritage").ToHashSet();
		if (mergedPillarsDict.TryGetValue(heritageId, out var mergedHeritageId)) {
			return heritages.FirstOrDefault(p => p.Id == mergedHeritageId);
		}
		
		return heritages.FirstOrDefault(p => p.Id == heritageId);
	}
	
	public Pillar? GetLanguageForId(string languageId) {
		var languages = this.Where(p => p.Type == "language").ToHashSet();
		if (mergedPillarsDict.TryGetValue(languageId, out var mergedLanguageId)) {
			return languages.FirstOrDefault(p => p.Id == mergedLanguageId);
		}
		
		return languages.FirstOrDefault(p => p.Id == languageId);
	}

	public void LoadPillars(ModFilesystem ck3ModFS) {
		var parser = new Parser();
		parser.RegisterRegex(CommonRegexes.String, (reader, pillarId) => LoadPillar(pillarId, reader));
		parser.IgnoreAndLogUnregisteredItems();
		parser.ParseGameFolder("common/culture/pillars", ck3ModFS, "txt", true);
	}

	public void LoadConverterPillars(string converterPillarsPath) {
		var parser = new Parser();
		parser.RegisterRegex(CommonRegexes.String, (reader, pillarId) => LoadPillar(pillarId, reader));
		parser.IgnoreAndLogUnregisteredItems();
		parser.ParseFolder(converterPillarsPath, "txt", true, logFilePaths: true);
		
		Logger.Debug($"Ignored mod flags when loading pillars: {ignoredModFlags}");
	}
	
	private void LoadPillar(string pillarId, BufferedReader pillarReader) {
		pillarData = new PillarData();
		
		pillarDataParser.ParseStream(pillarReader);

		if (pillarData.InvalidatingPillarIds.Any()) {
			foreach (var existingPillar in this) {
				if (!pillarData.InvalidatingPillarIds.Contains(existingPillar.Id)) {
					continue;
				}
				Logger.Debug($"Pillar {pillarId} is invalidated by existing {existingPillar.Id}.");
				mergedPillarsDict[pillarId] = existingPillar.Id;
				return;
			}
			Logger.Debug($"Loading optional pillar {pillarId}...");
		}
		if (pillarData.Type is null) {
			Logger.Warn($"Pillar {pillarId} has no type defined! Skipping.");
			return;
		}
		AddOrReplace(new Pillar(pillarId, pillarData));
	}

	private void InitPillarDataParser(ColorFactory colorFactory, ICollection<string> ck3ModFlags) {
		pillarDataParser.RegisterKeyword("REPLACED_BY", reader => LoadInvalidatingPillarIds(ck3ModFlags, reader));
		pillarDataParser.RegisterKeyword("type", reader => {
			pillarData.Type = reader.GetString();
		});
		pillarDataParser.RegisterKeyword("color", reader => {
			try {
				pillarData.Color = colorFactory.GetColor(reader);
			} catch (Exception e) {
				Logger.Warn($"Found invalid color when parsing pillar! {e.Message}");
			}
		});
		pillarDataParser.RegisterRegex(CommonRegexes.String, (reader, keyword) => {
			pillarData.Attributes.Add(new KeyValuePair<string, StringOfItem>(keyword, reader.GetStringOfItem()));
		});
		pillarDataParser.IgnoreAndLogUnregisteredItems();
	}
	
	private void LoadInvalidatingPillarIds(ICollection<string> ck3ModFlags, BufferedReader reader) {
		var pillarIdsPerModFlagParser = new Parser();
		
		if (ck3ModFlags.Count == 0) {
			pillarIdsPerModFlagParser.RegisterKeyword("vanilla", modPillarIdsReader => {
				pillarData.InvalidatingPillarIds = modPillarIdsReader.GetStrings();
			});
		} else {
			foreach (var modFlag in ck3ModFlags) {
				pillarIdsPerModFlagParser.RegisterKeyword(modFlag, modPillarIdsReader => {
					pillarData.InvalidatingPillarIds = modPillarIdsReader.GetStrings();
				});
			}
		}
		
		// Ignore pillar IDs from mods that haven't been selected.
		pillarIdsPerModFlagParser.IgnoreAndStoreUnregisteredItems(ignoredModFlags);
		pillarIdsPerModFlagParser.ParseStream(reader);
	}
	
	private PillarData pillarData = new();
	private readonly Parser pillarDataParser = new();
	
	private readonly IgnoredKeywordsSet ignoredModFlags = [];
}