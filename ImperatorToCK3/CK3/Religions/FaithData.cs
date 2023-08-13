using commonItems;
using commonItems.Colors;
using System.Collections.Generic;

namespace ImperatorToCK3.CK3.Religions; 

public record FaithData {
	public FaithData() { }
	public FaithData(IEnumerable<string> InvalidatingFaithIds, Color? Color, string? ReligiousHeadTitleId, List<string> DoctrineIds, List<string> HolySiteIds, List<KeyValuePair<string, StringOfItem>> Attributes) {
		this.Color = Color;
		this.ReligiousHeadTitleId = ReligiousHeadTitleId;
		this.DoctrineIds = DoctrineIds;
		this.HolySiteIds = HolySiteIds;
		this.Attributes = Attributes;
		this.InvalidatingFaithIds = InvalidatingFaithIds;
	}

	public IEnumerable<string> InvalidatingFaithIds { get; set; } = new List<string>();
	public Color? Color { get; set; }
	public string? ReligiousHeadTitleId { get; set; }
	public IList<string> DoctrineIds { get; } = new List<string>();
	public IList<string> HolySiteIds { get; } = new List<string>();

	public IList<KeyValuePair<string, StringOfItem>> Attributes { get; set; } = new List<KeyValuePair<string, StringOfItem>>();
}