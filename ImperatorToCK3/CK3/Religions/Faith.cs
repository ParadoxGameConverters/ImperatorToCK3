using commonItems;
using commonItems.Collections;
using commonItems.Colors;
using commonItems.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ImperatorToCK3.CK3.Religions;

internal sealed class Faith : IIdentifiable<string>, IPDXSerializable {
	/// <summary>
	/// CK3 allows a faith to have at most 3 eminent holy sites by default
	/// (see the FAITH_EMINENT_HOLY_SITES_MAX_DEFAULT define).
	/// </summary>
	public const int DefaultEminentHolySitesMax = 3;

	public string Id { get; }
	public Religion Religion { get; set; }
	public Color? Color { get; }
	public string? ReligiousHeadTitleId { get; }
	public string? HeadOfRiteTitleId { get; }
	/// <summary>
	/// Title of the head of the faith, if any.
	/// In CK3 1.20+ this can be defined as religious_head or head_of_rite in faith_details.
	/// </summary>
	public string? HeadTitleId => ReligiousHeadTitleId ?? HeadOfRiteTitleId;
	public string? MainRiteId { get; set; }
	public OrderedSet<string> DoctrineIds { get; }

	public Faith(string id, FaithData faithData, Religion religion) {
		Id = id;
		Religion = religion;
		
		Color = faithData.Color;
		ReligiousHeadTitleId = faithData.ReligiousHeadTitleId;
		HeadOfRiteTitleId = faithData.HeadOfRiteTitleId;
		MainRiteId = faithData.MainRiteId;
		DoctrineIds = faithData.DoctrineIds.ToOrderedSet();
		holySiteIds = faithData.HolySiteIds.ToOrderedSet();
		eminentHolySiteIds = faithData.EminentHolySiteIds.ToHashSet(StringComparer.Ordinal);
		// Eminent holy sites are part of the faith's holy sites too, but are not listed in the holy_sites block.
		foreach (var eminentHolySiteId in faithData.EminentHolySiteIds) {
			holySiteIds.Add(eminentHolySiteId);
		}
		localization = new(faithData.Localization);
		detailsAttributes = new(faithData.DetailsAttributes);
		attributes = [.. faithData.Attributes];

		// Fixup for issue found in TFE: add reformed_icon if faith has unreformed_faith_doctrine.
		if (DoctrineIds.Contains("unreformed_faith_doctrine") && !detailsAttributes.ContainsKey("reformed_icon")) {
			// Use the icon attribute.
			if (detailsAttributes.TryGetValue("icon", out var icon)) {
				detailsAttributes["reformed_icon"] = icon;
			}
		}

		PromoteLegacyHolySites(hasEminentHolySitesBlock: faithData.HasEminentHolySitesBlock);
		
		// Fix a faith having more doctrines in the same category than allowed.
		foreach (var category in religion.ReligionCollection.DoctrineGroups) {
			var categoryDoctrineSet = category.DoctrineIds.ToHashSet(StringComparer.Ordinal);
			var doctrinesInCategory = new List<string>();
			foreach (var doctrineId in DoctrineIds) {
				if (categoryDoctrineSet.Contains(doctrineId)) {
					doctrinesInCategory.Add(doctrineId);
				}
			}

			if (doctrinesInCategory.Count > category.NumberOfPicks) {
				Logger.Warn($"Faith {Id} has too many doctrines in category {category.Id}: " +
				            $"{string.Join(", ", doctrinesInCategory)}. Keeping the last {category.NumberOfPicks} of them.");
				
				DoctrineIds.ExceptWith(doctrinesInCategory);
				int kept = 0;
				for (int i = doctrinesInCategory.Count - 1; i >= 0 && kept < category.NumberOfPicks; --i) {
					DoctrineIds.Add(doctrinesInCategory[i]);
					++kept;
				}
			}
		}
	}

	private readonly OrderedSet<string> holySiteIds;
	private readonly HashSet<string> eminentHolySiteIds;
	private readonly KeyValuePair<string, StringOfItem>[] attributes;
	private readonly OrderedDictionary<string, StringOfItem> detailsAttributes;
	private readonly OrderedDictionary<string, StringOfItem> localization;

	/// <summary>
	/// All holy sites of the faith, both regular and eminent.
	/// </summary>
	public IReadOnlyCollection<string> HolySiteIds => [.. holySiteIds];
	public IReadOnlyCollection<string> EminentHolySiteIds => holySiteIds.Where(eminentHolySiteIds.Contains).ToArray();

	/// <summary>
	/// Faiths defined in the legacy format have no eminent_holy_sites block. In CK3 1.20+ only eminent holy sites
	/// give faith-wide bonuses, so promote up to 3 holy sites to keep the old faith-wide effects working.
	/// </summary>
	private void PromoteLegacyHolySites(bool hasEminentHolySitesBlock) {
		if (hasEminentHolySitesBlock || eminentHolySiteIds.Count > 0) {
			return;
		}
		foreach (var holySiteId in holySiteIds.Take(DefaultEminentHolySitesMax)) {
			eminentHolySiteIds.Add(holySiteId);
		}
	}

	public void ReplaceHolySiteId(string oldId, string newId) {
		if (!holySiteIds.Remove(oldId)) {
			Logger.Warn($"{oldId} does not belong to holy sites of faith {Id} and cannot be replaced!");
			return;
		}
		holySiteIds.Add(newId);

		if (eminentHolySiteIds.Remove(oldId)) {
			eminentHolySiteIds.Add(newId);
		}
	}

	public string Serialize(string indent, bool withBraces) {
		var contentIndent = indent;
		if (withBraces) {
			contentIndent += '\t';
		}

		var sb = new StringBuilder();
		if (withBraces) {
			sb.AppendLine("{");
		}

		sb.Append(contentIndent).AppendLine("faith_details = {");
		var detailsIndent = contentIndent + '\t';
		sb.Append(detailsIndent).AppendLine($"religion = {Religion.Id}");
		if (Color is not null) {
			sb.Append(detailsIndent).AppendLine($"color = {Color.Value.OutputRgb()}");
		}
		if (ReligiousHeadTitleId is not null) {
			sb.Append(detailsIndent).AppendLine($"religious_head = {ReligiousHeadTitleId}");
		}
		foreach (var (key, value) in detailsAttributes) {
			sb.Append(detailsIndent).AppendLine($"{key} = {value}");
		}
		sb.Append(contentIndent).AppendLine("}");

		if (MainRiteId is not null) {
			sb.Append(contentIndent).AppendLine($"main_rite = {MainRiteId}");
		}

		var regularHolySiteIds = holySiteIds.Where(id => !eminentHolySiteIds.Contains(id)).ToArray();
		AppendIdList(sb, contentIndent, "holy_sites", regularHolySiteIds);
		AppendIdList(sb, contentIndent, "eminent_holy_sites", EminentHolySiteIds);

		AppendDoctrinesAndTenets(sb, contentIndent, DoctrineIds, Religion.ReligionCollection.IsTenet);

		sb.Append(contentIndent).AppendLine("localization = {");
		foreach (var (key, value) in localization) {
			sb.Append(contentIndent).Append('\t').AppendLine($"{key}={value}");
		}
		sb.Append(contentIndent).AppendLine("}");

		sb.AppendLine(PDXSerializer.Serialize(attributes, indent: contentIndent, withBraces: false));

		if (withBraces) {
			sb.Append(indent).Append('}');
		}

		return sb.ToString();
	}

	/// <summary>
	/// In CK3 1.20+ tenets are defined separately from doctrines.
	/// </summary>
	internal static void AppendDoctrinesAndTenets(
		StringBuilder sb,
		string contentIndent,
		IEnumerable<string> doctrineAndTenetIds,
		Func<string, bool> isTenet
	) {
		var ids = doctrineAndTenetIds.ToArray();
		var tenetIds = ids.Where(isTenet).ToArray();
		var doctrineIds = ids.Where(id => !isTenet(id)).ToArray();

		if (tenetIds.Length > 0) {
			AppendIdList(sb, contentIndent, "tenets", tenetIds);
		}
		if (doctrineIds.Length > 0) {
			AppendIdList(sb, contentIndent, "doctrines", doctrineIds);
		}
	}

	/// <summary>
	/// Writes a list of IDs one per line.
	/// </summary>
	private static void AppendIdList(StringBuilder sb, string contentIndent, string keyword, IEnumerable<string> ids) {
		sb.Append(contentIndent).AppendLine($"{keyword} = {{");
		foreach (var id in ids) {
			sb.Append(contentIndent).Append('\t').AppendLine(id);
		}
		sb.Append(contentIndent).AppendLine("}");
	}

	public OrderedSet<string> GetDoctrineIdsForDoctrineGroupId(string doctrineGroupId) {
		if (Religion.ReligionCollection.DoctrineGroups.TryGetValue(doctrineGroupId, out var group)) {
			return GetDoctrineIdsForDoctrineGroup(group);
		}

		Logger.Warn($"Doctrine group {doctrineGroupId} not found.");
		return [];
	}

	private OrderedSet<string> GetDoctrineIdsForDoctrineGroup(DoctrineGroup group) {
		var potentialDoctrineIds = group.DoctrineIds;
		var religionCollection = Religion.ReligionCollection;

		// In CK3 1.20+, doctrines of the main rite take precedence over faith doctrines.
		if (MainRiteId is not null && religionCollection.Rites.TryGetValue(MainRiteId, out var mainRite)) {
			var matchingInRite = mainRite.DoctrineIds.Intersect(potentialDoctrineIds).ToOrderedSet();
			if (matchingInRite.Count != 0) {
				return matchingInRite;
			}
		}

		// Look in faith next. If not found, look in religion.
		var matchingInFaith = DoctrineIds.Intersect(potentialDoctrineIds).ToOrderedSet();
		if (matchingInFaith.Count != 0) {
			return matchingInFaith;
		}

		return Religion.DoctrineIds.Intersect(potentialDoctrineIds).ToOrderedSet();
	}

	public bool HasDoctrine(string doctrineId) {
		var group = Religion.ReligionCollection.DoctrineGroups
			.FirstOrDefault(group => group.DoctrineIds.Contains(doctrineId));
		return group is not null && GetDoctrineIdsForDoctrineGroup(group).Contains(doctrineId);
	}
}