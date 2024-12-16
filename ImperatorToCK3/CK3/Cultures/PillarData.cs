using commonItems;
using commonItems.Colors;
using System.Collections.Generic;

namespace ImperatorToCK3.CK3.Cultures;

internal record PillarData {
	public List<string> InvalidatingPillarIds { get; set; } = [];
	public string? Type { get; set; }
	public Color? Color { get; set; }

	public List<KeyValuePair<string, StringOfItem>> Attributes { get; } = [];
}