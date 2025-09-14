using commonItems;
using commonItems.Collections;
using commonItems.Colors;
using commonItems.Serialization;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ImperatorToCK3.CK3.Religions;

internal sealed class Faith : IIdentifiable<string>, IPDXSerializable {
	public string Id { get; }
	public Religion Religion { get; set; }
	public Color? Color { get; }
	public string? ReligiousHeadTitleId { get; }
	public OrderedSet<string> DoctrineIds { get; }

	public Faith(string id, FaithData faithData, Religion religion) {
		Id = id;
		Religion = religion;
		
		Color = faithData.Color;
		ReligiousHeadTitleId = faithData.ReligiousHeadTitleId;
		DoctrineIds = faithData.DoctrineIds.ToOrderedSet();
		holySiteIds = faithData.HolySiteIds.ToOrderedSet();
		attributes = [.. faithData.Attributes];

		// Fixup for issue found in TFE: add reformed_icon if faith has unreformed_faith_doctrine.
		if (DoctrineIds.Contains("unreformed_faith_doctrine") && attributes.All(pair => pair.Key != "reformed_icon")) {
			// Use the icon attribute.
			var icon = attributes.FirstOrDefault(pair => pair.Key == "icon");
			attributes = [.. attributes, new KeyValuePair<string, StringOfItem>("reformed_icon", icon.Value)];
		}
		
		// Fix a faith having more doctrines in the same category than allowed.
		foreach (var category in religion.ReligionCollection.DoctrineCategories) {
			var doctrinesInCategory = DoctrineIds.Where(d => category.DoctrineIds.Contains(d)).ToArray();
			if (doctrinesInCategory.Length > category.NumberOfPicks) {
				Logger.Warn($"Faith {Id} has too many doctrines in category {category.Id}: " +
				            $"{string.Join(", ", doctrinesInCategory)}. Keeping the last {category.NumberOfPicks} of them.");
				
				DoctrineIds.ExceptWith(doctrinesInCategory);
				foreach (var doctrine in doctrinesInCategory.Reverse().Take(category.NumberOfPicks)) {
					DoctrineIds.Add(doctrine);
				}
			}
		}
	}

	private readonly OrderedSet<string> holySiteIds;
	public IReadOnlyCollection<string> HolySiteIds => [.. holySiteIds];
	private readonly KeyValuePair<string, StringOfItem>[] attributes;

	public void ReplaceHolySiteId(string oldId, string newId) {
		if (holySiteIds.Remove(oldId)) {
			holySiteIds.Add(newId);
		} else {
			Logger.Warn($"{oldId} does not belong to holy sites of faith {Id} and cannot be replaced!");
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

		if (Color is not null) {
			sb.Append(contentIndent).AppendLine($"color={Color.OutputRgb()}");
		}
		if (ReligiousHeadTitleId is not null) {
			sb.Append(contentIndent).AppendLine($"religious_head={ReligiousHeadTitleId}");
		}
		foreach (var holySiteId in HolySiteIds) {
			sb.Append(contentIndent).AppendLine($"holy_site={holySiteId}");
		}
		foreach (var doctrineId in DoctrineIds) {
			sb.Append(contentIndent).AppendLine($"doctrine={doctrineId}");
		}
		
		sb.AppendLine(PDXSerializer.Serialize(attributes, indent: contentIndent, withBraces: false));

		if (withBraces) {
			sb.Append(indent).Append('}');
		}

		return sb.ToString();
	}

	public OrderedSet<string> GetDoctrineIdsForDoctrineCategoryId(string doctrineCategoryId) {
		if (!Religion.ReligionCollection.DoctrineCategories.TryGetValue(doctrineCategoryId, out var category)) {
			Logger.Warn($"Doctrine category {doctrineCategoryId} not found.");
			return [];
		}
		
		return GetDoctrineIdsForDoctrineCategory(category);
	}

	private OrderedSet<string> GetDoctrineIdsForDoctrineCategory(DoctrineCategory category) {
		var potentialDoctrineIds = category.DoctrineIds;

		// Look in faith first. If not found, look in religion.
		var matchingInFaith = DoctrineIds.Intersect(potentialDoctrineIds).ToOrderedSet();
		if (matchingInFaith.Count != 0) {
			return matchingInFaith;
		}

		return Religion.DoctrineIds.Intersect(potentialDoctrineIds).ToOrderedSet();
	}
	
	public bool HasDoctrine(string doctrineId) {
		var category = Religion.ReligionCollection.DoctrineCategories
			.FirstOrDefault(category => category.DoctrineIds.Contains(doctrineId));
		if (category is null) {
			return false;
		}
		
		return GetDoctrineIdsForDoctrineCategory(category).Contains(doctrineId);
	}
}