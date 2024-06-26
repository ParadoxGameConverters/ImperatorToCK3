using commonItems;
using System.Collections.Generic;

namespace ImperatorToCK3.Mappers.DeathReason;

public sealed class DeathReasonMapper {
	public DeathReasonMapper() {
		Logger.Info("Parsing death reason mappings...");
		var parser = new Parser();
		RegisterKeys(parser);
		parser.ParseFile("configurables/deathMappings.txt");
		Logger.Info($"Loaded {irToCK3ReasonMap.Count} death reason links.");

		Logger.IncrementProgress();
	}
	public DeathReasonMapper(BufferedReader reader) {
		var parser = new Parser();
		RegisterKeys(parser);
		parser.ParseStream(reader);
	}
	public string? GetCK3ReasonForImperatorReason(string irReason) {
		return irToCK3ReasonMap.TryGetValue(irReason, out var value) ? value : null;
	}

	private void RegisterKeys(Parser parser) {
		parser.RegisterKeyword("link", reader => {
			var mapping = new DeathReasonMapping(reader);
			if (mapping.Ck3Reason is null) {
				return;
			}

			foreach (var impReason in mapping.ImperatorReasons) {
				irToCK3ReasonMap.Add(impReason, mapping.Ck3Reason);
			}
		});
	}
	private readonly Dictionary<string, string> irToCK3ReasonMap = new();
}