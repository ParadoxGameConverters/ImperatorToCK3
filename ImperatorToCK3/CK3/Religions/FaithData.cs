using commonItems;
using commonItems.Colors;
using System.Collections.Generic;

namespace ImperatorToCK3.CK3.Religions; 

public record FaithData {
	public IEnumerable<string> InvalidatingFaithIds { get; set; } = new List<string>();
	public Color? Color { get; set; }
	public string? ReligiousHeadTitleId { get; set; }
	public IList<string> DoctrineIds { get; } = new List<string>();
	public IList<string> HolySiteIds { get; init; } = new List<string>();

	public IList<KeyValuePair<string, StringOfItem>> Attributes { get; init; } = new List<KeyValuePair<string, StringOfItem>>();
}