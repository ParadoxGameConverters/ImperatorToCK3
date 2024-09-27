using commonItems;
using System.Collections.Generic;

namespace ImperatorToCK3.Mappers.Artifact; 

public class ArtifactVisualsMapper {
	public ArtifactVisualsMapper(string mappingsPath) {  // TODO: use this somewhere
		Logger.Info("Loading artifact visuals mappings...");
		var parser = new Parser();
		RegisterKeys(parser);
		parser.ParseFile(mappingsPath);
		Logger.Info($"Loaded {impToCK3VisualMap.Count} artifact visuals links.");
	}
	
	private void RegisterKeys(Parser parser) {
		parser.RegisterKeyword("link", reader => {
			var mapping = new ArtifactVisualsMapping(reader);
			if (mapping.Ck3Visual is null) {
				return;
			}
			
			foreach (var impVisual in mapping.ImpVisuals) {
				impToCK3VisualMap.Add(impVisual, mapping.Ck3Visual);
			}
		});
	}
	
	private readonly Dictionary<string, string> impToCK3VisualMap = new();
}