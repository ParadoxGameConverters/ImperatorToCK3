using commonItems;
using System.Collections.Generic;

namespace ImperatorToCK3.Mappers.Artifact; 

public class ArtifactVisualsMapper {
	public ArtifactVisualsMapper(string mappingsPath) {  // TODO: use this somewhere
		Logger.Info("Loading artifact visuals mappings...");
		var parser = new Parser();
		RegisterKeys(parser);
		parser.ParseFile(mappingsPath);
		
		Logger.Info($"Loaded {irTreasureToCK3VisualMap.Count} treasure to visual links " +
		            $"and {irIconToCK3VisualMap.Count} icon to visual links.");
	}
	
	private void RegisterKeys(Parser parser) {
		parser.RegisterKeyword("link", linkReader => {
			string? ck3Visual = null;
			var irTreasureIds = new List<string>();
			var irIconIds = new List<string>();

			var linkParser = new Parser();
			linkParser.RegisterKeyword("ck3Visual", reader => {
				ck3Visual = reader.GetString();
			});
			linkParser.RegisterKeyword("irTreasure" , reader => {
				irTreasureIds.Add(reader.GetString());
			});
			linkParser.RegisterKeyword("irIcon" , reader => {
				irIconIds.Add(reader.GetString());
			});
			linkParser.IgnoreAndLogUnregisteredItems();
			linkParser.ParseStream(linkReader);

			if (ck3Visual is null) {
				return;
			}
			
			foreach (var irTreasureId in irTreasureIds) {
				irTreasureToCK3VisualMap.Add(irTreasureId, ck3Visual);
			}
			foreach (var irIconId in irIconIds) {
				irIconToCK3VisualMap.Add(irIconId, ck3Visual);
			}
		});
	}
	
	private readonly Dictionary<string, string> irTreasureToCK3VisualMap = new();
	private readonly Dictionary<string, string> irIconToCK3VisualMap = new();
}