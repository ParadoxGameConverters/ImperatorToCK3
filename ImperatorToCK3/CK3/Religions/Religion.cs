using commonItems;
using commonItems.Collections;
using commonItems.Serialization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace ImperatorToCK3.CK3.Religions;

internal sealed class Religion : IIdentifiable<string>, IPDXSerializable {
	// Defaults from CK3's NRigion defines.
	public const int DefaultEminentHolySitesMax = 3;
	public const int DefaultEminentHolySitesMin = 1;
	public const int DefaultHolySitesMax = 9;
	public const int DefaultHolySitesMin = 1;

	public string Id { get; }
	public OrderedSet<string> DoctrineIds { get; } = [];

	public ReligionCollection ReligionCollection { get; }
	private readonly OrderedDictionary<string, StringOfItem> localization = [];
	private readonly OrderedDictionary<string, StringOfItem> detailsAttributes = [];

	public Religion(string id, BufferedReader religionReader, ReligionCollection religions) {
		Id = id;
		ReligionCollection = religions;

		var religionParser = new Parser(implicitVariableHandling: true);
		religionParser.RegisterKeyword("doctrine", reader => DoctrineIds.Add(reader.GetString()));
		// CK3 1.20+ religions define their family and other details in a religion_details block.
		religionParser.RegisterKeyword("religion_details", reader => {
			foreach (var assignment in reader.GetAssignments()) {
				detailsAttributes[assignment.Key] = new StringOfItem(assignment.Value);
			}
		});
		religionParser.RegisterKeyword("localization", reader => {
			var localizationParser = new Parser(implicitVariableHandling: true);
			localizationParser.RegisterRegex(CommonRegexes.Catchall, (locReader, locKey) => {
				localization[locKey] = locReader.GetStringOfItem();
			});
			localizationParser.ParseStream(reader);
		});
		religionParser.RegisterRegex(CommonRegexes.Catchall, (reader, keyword) => {
			attributes.Add(new KeyValuePair<string, StringOfItem>(keyword, reader.GetStringOfItem()));
		});
		religionParser.ParseStream(religionReader);

		ReadHolySiteLimits();

		// Fix a religion having more than one doctrine in the same group.
		foreach (var group in religions.DoctrineGroups) {
			var doctrinesInGroup = DoctrineIds.Intersect(group.DoctrineIds).ToList();
			if (doctrinesInGroup.Count <= 1) {
				continue;
			}

			Logger.Warn($"Religion {Id} has too many doctrines in group {group.Id}: " +
			            $"{string.Join(", ", doctrinesInGroup)}. Keeping the last of them.");

			DoctrineIds.ExceptWith(doctrinesInGroup);
			DoctrineIds.Add(doctrinesInGroup[^1]);
		}
	}

	private void ReadHolySiteLimits() {
		EminentHolySitesMax = GetAttributeInt("eminent_holy_sites_max", DefaultEminentHolySitesMax);
		EminentHolySitesMin = GetAttributeInt("eminent_holy_sites_min", DefaultEminentHolySitesMin);
		HolySitesMax = GetAttributeInt("holy_sites_max", DefaultHolySitesMax);
		HolySitesMin = GetAttributeInt("holy_sites_min", DefaultHolySitesMin);

		var mainHolySiteId = GetAttributeString("main_holy_site");
		MainHolySiteId = mainHolySiteId == "none" ? null : mainHolySiteId;

		if (HolySitesMin > HolySitesMax) {
			Logger.Warn($"Religion {Id} has holy_sites_min ({HolySitesMin}) greater than holy_sites_max ({HolySitesMax}).");
		}
		if (EminentHolySitesMax > HolySitesMax) {
			Logger.Warn($"Religion {Id} has eminent_holy_sites_max ({EminentHolySitesMax}) greater than holy_sites_max ({HolySitesMax}). The latter will cap eminent holy sites.");
		}
		if (EminentHolySitesMin > EffectiveEminentHolySitesMax) {
			Logger.Warn($"Religion {Id} has eminent_holy_sites_min ({EminentHolySitesMin}) greater than the effective eminent holy site max ({EffectiveEminentHolySitesMax}).");
		}
	}

	private string? GetAttributeString(string key) {
		for (int i = attributes.Count - 1; i >= 0; --i) {
			if (attributes[i].Key == key) {
				return attributes[i].Value.ToString().RemQuotes();
			}
		}
		return null;
	}

	private int GetAttributeInt(string key, int defaultValue) {
		var valueStr = GetAttributeString(key);
		if (valueStr is null) {
			return defaultValue;
		}

		if (int.TryParse(valueStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) && value >= 0) {
			return value;
		}

		Logger.Warn($"Religion {Id} has invalid {key}: {valueStr}. Using default {defaultValue}.");
		return defaultValue;
	}

	/// <summary>
	/// ID of the religion family, e.g. rf_abrahamic. Defined in the religion_details block in CK3 1.20+.
	/// </summary>
	public string? FamilyId => detailsAttributes.TryGetValue("family", out var family) ? family.ToString().RemQuotes() : null;

	public int EminentHolySitesMax { get; private set; } = DefaultEminentHolySitesMax;
	public int EminentHolySitesMin { get; private set; } = DefaultEminentHolySitesMin;
	public int HolySitesMax { get; private set; } = DefaultHolySitesMax;
	public int HolySitesMin { get; private set; } = DefaultHolySitesMin;

	/// <summary>
	/// The total holy site limit includes eminent holy sites, so it can also cap the eminent limit.
	/// </summary>
	public int EffectiveEminentHolySitesMax => Math.Min(EminentHolySitesMax, HolySitesMax);
	public int EffectiveEminentHolySitesMin => Math.Min(EminentHolySitesMin, EffectiveEminentHolySitesMax);

	/// <summary>
	/// Optional key of a holy site that cannot be removed from any faith of this religion.
	/// </summary>
	public string? MainHolySiteId { get; private set; }

	public IdObjectCollection<string, Faith> Faiths { get; } = [];
	private readonly List<KeyValuePair<string, StringOfItem>> attributes = [];

	public string Serialize(string indent, bool withBraces) {
		var contentIndent = indent;
		if (withBraces) {
			contentIndent += '\t';
		}

		var sb = new StringBuilder();
		if (withBraces) {
			sb.AppendLine("{");
		}

		sb.Append(contentIndent).AppendLine("religion_details = {");
		var detailsIndent = contentIndent + '\t';
		foreach (var (key, value) in detailsAttributes) {
			sb.Append(detailsIndent).AppendLine($"{key} = {value}");
		}
		sb.Append(contentIndent).AppendLine("}");

		foreach (var doctrineId in DoctrineIds) {
			sb.Append(contentIndent).AppendLine($"doctrine={doctrineId}");
		}

		sb.Append(contentIndent).AppendLine("localization = {");
		foreach (var (key, value) in localization) {
			sb.Append(contentIndent).Append('\t').AppendLine($"{key}={value}");
		}
		sb.Append(contentIndent).AppendLine("}");

		sb.AppendLine(PDXSerializer.Serialize(attributes, indent: contentIndent+'\t', withBraces: false));

		if (withBraces) {
			sb.Append(indent).Append('}');
		}

		return sb.ToString();
	}
}