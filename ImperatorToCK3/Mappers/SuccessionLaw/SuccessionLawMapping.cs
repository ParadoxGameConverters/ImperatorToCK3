using commonItems;
using commonItems.Collections;
using System.Collections.Generic;

namespace ImperatorToCK3.Mappers.SuccessionLaw;

internal sealed class SuccessionLawMapping {
	private string ImperatorLaw { get; set; } = string.Empty;
	private OrderedSet<string> CK3SuccessionLaws { get; } = [];
	private HashSet<string> ImperatorGovernments { get; set; } = [];
	private HashSet<string> RequiredCK3Dlcs { get; } = [];
	
	public SuccessionLawMapping(BufferedReader mappingReader) {
		var parser = new Parser();
		parser.RegisterKeyword("ir", reader => ImperatorLaw = reader.GetString());
		parser.RegisterKeyword("ck3", reader => CK3SuccessionLaws.Add(reader.GetString()));
		parser.RegisterKeyword("ir_government", reader => ImperatorGovernments.Add(reader.GetString()));
		parser.RegisterKeyword("has_ck3_dlc", reader => RequiredCK3Dlcs.Add(reader.GetString()));
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		parser.ParseStream(mappingReader);
		
		
		if (CK3SuccessionLaws.Count == 0) {
			Logger.Warn("SuccessionLawMapper: link with no CK3 successions laws");
		}
	}
	
	internal OrderedSet<string>? Match(string irLaw, string? irGovernment, IReadOnlyCollection<string> enabledCK3Dlcs) {
		if (irLaw != ImperatorLaw) {
			return null;
		}
		
		if (ImperatorGovernments.Count != 0) {
			if (irGovernment is null) {
				return null;
			}
			if (!ImperatorGovernments.Contains(irGovernment)) {
				return null;
			}
		}
		
		if (RequiredCK3Dlcs.Count != 0) {
			if (enabledCK3Dlcs.Count == 0) {
				return null;
			}
			if (!RequiredCK3Dlcs.IsSubsetOf(enabledCK3Dlcs)) {
				return null;
			}
		}

		return new(CK3SuccessionLaws);
	}
}