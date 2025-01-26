using commonItems;
using System.Collections.Generic;

namespace ImperatorToCK3.Mappers.Artifact; 

public class ArtifactMapper {
	public ArtifactMapper(string mappingsPath) {  // TODO: use this somewhere
		Logger.Info("Loading artifact mappings...");
		var parser = new Parser();
		RegisterKeys(parser);
		parser.ParseFile(mappingsPath);
		
		Logger.Info($"Loaded {mappings.Count} artifact mappings.");
		
		// TODO: implement checking if the ck3 visuals actually exist. We need to read the CK3 visuals files for that.
	}
	
	private void RegisterKeys(Parser parser) {
		parser.RegisterKeyword("link", linkReader => {
			string? ck3Visual = null;
			string? ck3Type = null;
			var irTreasureIds = new List<string>();
			var irIconIds = new List<string>();

			var linkParser = new Parser();
			linkParser.RegisterKeyword("ck3Visual", reader => {
				ck3Visual = reader.GetString();
			});
			linkParser.RegisterKeyword("ck3Type", reader => {
				ck3Type = reader.GetString();
			});
			linkParser.RegisterKeyword("irTreasure" , reader => {
				irTreasureIds.Add(reader.GetString());
			});
			linkParser.RegisterKeyword("irIcon" , reader => {
				irIconIds.Add(reader.GetString());
			});
			linkParser.IgnoreAndLogUnregisteredItems();
			linkParser.ParseStream(linkReader);

			if (ck3Visual is null || ck3Type is null) {
				return;
			}

			var mapping = new ArtifactMapping() {
				CK3Visual = ck3Visual, CK3Type = ck3Type, IRTreasureIds = irTreasureIds, IRIconIds = irIconIds
			};
			mappings.Add(mapping);
		});
	}

	public (string?, string?)? GetVisualAndType(string irArtifactId, string irIconId) {
		foreach (var mapping in mappings) {
			if (mapping.IRTreasureIds.Count > 0 && !mapping.IRTreasureIds.Contains(irArtifactId)) {
				continue;
			}
			if (mapping.IRIconIds.Count > 0 && !mapping.IRIconIds.Contains(irIconId)) {
				continue;
			}
			
			return (mapping.CK3Visual, mapping.CK3Type);
		}
		
		return null;
	}

	private readonly List<ArtifactMapping> mappings = [];
}