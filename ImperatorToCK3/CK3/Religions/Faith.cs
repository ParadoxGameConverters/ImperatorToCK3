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

	public Faith(string id, FaithData faithData, Religion religion, bool isFromConverter = false) {
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
		ApplyHolySiteLimits(isFromConverter);
		
		// Fix a faith having more than one doctrine in the same group.
		foreach (var group in religion.ReligionCollection.DoctrineGroups) {
			var doctrinesInGroup = DoctrineIds.Intersect(group.DoctrineIds).ToList();
			if (doctrinesInGroup.Count <= 1) {
				continue;
			}

			Logger.Warn($"Faith {Id} has too many doctrines in group {group.Id}: " +
			            $"{string.Join(", ", doctrinesInGroup)}. Keeping the last of them.");

			DoctrineIds.ExceptWith(doctrinesInGroup);
			DoctrineIds.Add(doctrinesInGroup[^1]);
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
	/// give faith-wide bonuses, so promote up to the religion's limit to keep the old faith-wide effects working.
	/// </summary>
	private void PromoteLegacyHolySites(bool hasEminentHolySitesBlock) {
		if (hasEminentHolySitesBlock || eminentHolySiteIds.Count > 0) {
			return;
		}
		foreach (var holySiteId in GetHolySiteIdsForPromotion().Take(Religion.EffectiveEminentHolySitesMax)) {
			eminentHolySiteIds.Add(holySiteId);
		}
	}

	private void ApplyHolySiteLimits(bool isFromConverter) {
		if (!isFromConverter) {
			ValidateHolySiteLimits();
			return;
		}

		PromoteHolySitesToEminentMinimum();
		DemoteExcessEminentHolySites();
		TrimExcessHolySites();
		ValidateHolySiteMinimums();
	}

	private void PromoteHolySitesToEminentMinimum() {
		int minEminentSites = Religion.EffectiveEminentHolySitesMin;
		if (eminentHolySiteIds.Count >= minEminentSites) {
			return;
		}

		var sitesToPromote = GetHolySiteIdsForPromotion()
			.Where(holySiteId => !eminentHolySiteIds.Contains(holySiteId))
			.Take(minEminentSites - eminentHolySiteIds.Count)
			.ToArray();
		foreach (var holySiteId in sitesToPromote) {
			eminentHolySiteIds.Add(holySiteId);
		}
	}

	private void DemoteExcessEminentHolySites() {
		int maxEminentSites = Religion.EffectiveEminentHolySitesMax;
		int eminentCount = eminentHolySiteIds.Count;
		if (eminentCount <= maxEminentSites) {
			return;
		}

		var sitesToDemote = OrderEminentHolySitesForRetention()
			.Skip(maxEminentSites)
			.ToArray();
		foreach (var holySiteId in sitesToDemote) {
			eminentHolySiteIds.Remove(holySiteId);
		}

		Logger.Warn($"Faith {Id} has {eminentCount} eminent holy sites, but religion {Religion.Id} allows at most {maxEminentSites}. Demoted: {string.Join(", ", sitesToDemote)}.");
	}

	private void TrimExcessHolySites() {
		int maxHolySites = Religion.HolySitesMax;
		if (holySiteIds.Count <= maxHolySites) {
			return;
		}

		int excessCount = holySiteIds.Count - maxHolySites;
		var removableRegularSites = holySiteIds
			.Where(holySiteId => !eminentHolySiteIds.Contains(holySiteId) && holySiteId != Religion.MainHolySiteId)
			.Reverse()
			.ToArray();
		var removableEminentSites = holySiteIds
			.Where(holySiteId => eminentHolySiteIds.Contains(holySiteId) && holySiteId != Religion.MainHolySiteId)
			.Reverse()
			.ToArray();
		var sitesToRemove = removableRegularSites
			.Concat(removableEminentSites)
			.Take(excessCount)
			.ToArray();

		foreach (var holySiteId in sitesToRemove) {
			holySiteIds.Remove(holySiteId);
			eminentHolySiteIds.Remove(holySiteId);
		}

		if (sitesToRemove.Length < excessCount) {
			Logger.Warn($"Faith {Id} has {holySiteIds.Count + sitesToRemove.Length} holy sites, but religion {Religion.Id} allows at most {maxHolySites}. Could only remove {sitesToRemove.Length} holy sites while protecting {Religion.MainHolySiteId}.");
		} else {
			Logger.Warn($"Faith {Id} has more holy sites than religion {Religion.Id} allows ({maxHolySites}). Removed: {string.Join(", ", sitesToRemove)}.");
		}
	}

	private void ValidateHolySiteLimits() {
		int effectiveEminentMax = Religion.EffectiveEminentHolySitesMax;
		if (eminentHolySiteIds.Count > effectiveEminentMax) {
			Logger.Warn($"Faith {Id} has {eminentHolySiteIds.Count} eminent holy sites, but religion {Religion.Id} allows at most {effectiveEminentMax}.");
		}
		if (holySiteIds.Count > Religion.HolySitesMax) {
			Logger.Warn($"Faith {Id} has {holySiteIds.Count} holy sites, but religion {Religion.Id} allows at most {Religion.HolySitesMax}.");
		}
		if (holySiteIds.Count == 0) {
			return;
		}
		if (eminentHolySiteIds.Count < Religion.EffectiveEminentHolySitesMin) {
			Logger.Debug($"Faith {Id} has {eminentHolySiteIds.Count} eminent holy sites, but religion {Religion.Id} requires at least {Religion.EffectiveEminentHolySitesMin}.");
		}
		if (holySiteIds.Count < Religion.HolySitesMin) {
			Logger.Debug($"Faith {Id} has {holySiteIds.Count} holy sites, but religion {Religion.Id} requires at least {Religion.HolySitesMin}.");
		}
	}

	private void ValidateHolySiteMinimums() {
		if (eminentHolySiteIds.Count < Religion.EffectiveEminentHolySitesMin) {
			Logger.Warn($"Faith {Id} has only {eminentHolySiteIds.Count} eminent holy sites, but religion {Religion.Id} requires at least {Religion.EffectiveEminentHolySitesMin}.");
		}
		if (holySiteIds.Count < Religion.HolySitesMin) {
			Logger.Warn($"Faith {Id} has only {holySiteIds.Count} holy sites, but religion {Religion.Id} requires at least {Religion.HolySitesMin}.");
		}
	}

	private IEnumerable<string> GetHolySiteIdsForPromotion() {
		if (Religion.MainHolySiteId is { } mainHolySiteId && holySiteIds.Contains(mainHolySiteId)) {
			yield return mainHolySiteId;
		}
		foreach (var holySiteId in holySiteIds) {
			if (holySiteId != Religion.MainHolySiteId) {
				yield return holySiteId;
			}
		}
	}

	private IEnumerable<string> OrderEminentHolySitesForRetention() {
		if (Religion.MainHolySiteId is { } mainHolySiteId && eminentHolySiteIds.Contains(mainHolySiteId)) {
			yield return mainHolySiteId;
		}
		foreach (var holySiteId in holySiteIds) {
			if (holySiteId != Religion.MainHolySiteId && eminentHolySiteIds.Contains(holySiteId)) {
				yield return holySiteId;
			}
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