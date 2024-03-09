using commonItems;
using commonItems.Collections;
using commonItems.Colors;
using System.Collections.Generic;

namespace ImperatorToCK3.CK3.Cultures;

public record CultureData {
	public IEnumerable<string> InvalidatingCultureIds { get; set; } = new List<string>();
	public Color? Color { get; set; }
	public OrderedSet<string> ParentCultureIds { get; set; } = new();
	public Pillar? Heritage { get; set; }
	public Pillar? Language { get; set; }
	public OrderedSet<string> TraditionIds { get; set; } = new();
	public OrderedSet<NameList> NameLists { get; } = new();

	public IList<KeyValuePair<string, StringOfItem>> Attributes { get; } = new List<KeyValuePair<string, StringOfItem>>();
}