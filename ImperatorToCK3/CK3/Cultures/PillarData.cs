using commonItems;
using System.Collections.Generic;

namespace ImperatorToCK3.CK3.Cultures;

public record PillarData {
	public IEnumerable<string> InvalidatingPillarIds { get; set; } = new List<string>();
	public string? Type { get; set; }
	
	public IList<KeyValuePair<string, StringOfItem>> Attributes { get; } = new List<KeyValuePair<string, StringOfItem>>();
}