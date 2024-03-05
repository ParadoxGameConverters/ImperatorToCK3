using commonItems;
using commonItems.Collections;
using commonItems.Colors;
using commonItems.Mods;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace ImperatorToCK3.CK3.Cultures; 

public class PillarCollection : IdObjectCollection<string, Pillar> {
	public IEnumerable<Pillar> Heritages => this.Where(p => p.Type == "heritage").ToImmutableList();

	public PillarCollection(ColorFactory colorFactory) {
		InitPillarDataParser(colorFactory);
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

	private void InitPillarDataParser(ColorFactory colorFactory) {
		pillarDataParser.RegisterKeyword("INVALIDATED_BY", reader => {
			pillarData.InvalidatingPillarIds = reader.GetStrings();
		});
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
	
	private PillarData pillarData = new();
	private readonly Parser pillarDataParser = new();
}