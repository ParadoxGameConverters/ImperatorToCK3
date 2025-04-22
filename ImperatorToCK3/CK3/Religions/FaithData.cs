using commonItems;
using commonItems.Colors;
using System.Collections.Generic;

namespace ImperatorToCK3.CK3.Religions; 

internal record FaithData {
	public List<string> InvalidatingFaithIds { get; set; } = [];
	public Color? Color { get; set; }
	public string? ReligiousHeadTitleId { get; set; }
	public List<string> DoctrineIds { get; } = [];
	public List<string> HolySiteIds { get; init; } = [];

	public List<KeyValuePair<string, StringOfItem>> Attributes { get; init; } = [];
}