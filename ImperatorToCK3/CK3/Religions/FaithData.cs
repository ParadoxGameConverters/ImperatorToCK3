using commonItems;
using commonItems.Colors;
using System.Collections.Generic;

namespace ImperatorToCK3.CK3.Religions;

internal sealed record FaithData {
	/// <summary>
	/// ID of the religion the faith belongs to. In CK3 1.20+ this is defined in the faith_details block.
	/// </summary>
	public string? ReligionId { get; set; }
	public List<string> InvalidatingFaithIds { get; set; } = [];
	public Color? Color { get; set; }
	public string? ReligiousHeadTitleId { get; set; }
	public string? HeadOfRiteTitleId { get; set; }
	public string? MainRiteId { get; set; }
	public List<string> DoctrineIds { get; } = [];
	public List<string> HolySiteIds { get; init; } = [];
	public List<string> EminentHolySiteIds { get; init; } = [];

	/// <summary>
	/// Whether the faith definition explicitly listed eminent holy sites.
	/// In CK3 1.20+ every faith has both holy_sites and eminent_holy_sites blocks.
	/// </summary>
	public bool HasEminentHolySitesBlock { get; set; }

	public OrderedDictionary<string, StringOfItem> Localization { get; } = [];

	/// <summary>
	/// Attributes of the faith_details block, like icon, reformed_icon or graphical_faith.
	/// </summary>
	public OrderedDictionary<string, StringOfItem> DetailsAttributes { get; init; } = [];

	/// <summary>
	/// Attributes defined directly in the faith block, like cultures, origin or historical.
	/// </summary>
	public List<KeyValuePair<string, StringOfItem>> Attributes { get; init; } = [];
}