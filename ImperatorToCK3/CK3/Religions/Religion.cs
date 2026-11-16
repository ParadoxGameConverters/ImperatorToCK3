using commonItems;
using commonItems.Collections;
using commonItems.Colors;
using commonItems.Serialization;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ImperatorToCK3.CK3.Religions;

internal sealed class Religion : IIdentifiable<string>, IPDXSerializable {
	public string Id { get; }
	public OrderedSet<string> DoctrineIds { get; } = [];

	public ReligionCollection ReligionCollection { get; }
	private readonly OrderedDictionary<string, StringOfItem> localization = [];
	private readonly OrderedDictionary<string, StringOfItem> detailsAttributes = [];
	private readonly FaithDataParser faithDataParser;

	public Religion(string id, BufferedReader religionReader, ReligionCollection religions, ColorFactory colorFactory) {
		Id = id;
		ReligionCollection = religions;
		faithDataParser = new FaithDataParser(colorFactory);

		var religionParser = new Parser(implicitVariableHandling: true);
		religionParser.RegisterKeyword("doctrine", reader => DoctrineIds.Add(reader.GetString()));
		// CK3 1.20+ religions define their family and other details in a religion_details block.
		religionParser.RegisterKeyword("religion_details", reader => {
			foreach (var assignment in reader.GetAssignments()) {
				detailsAttributes[assignment.Key] = new StringOfItem(assignment.Value);
			}
		});
		// Legacy keywords that belong in religion_details in CK3 1.20+.
		foreach (var keyword in DetailKeywords) {
			religionParser.RegisterKeyword(keyword, reader => detailsAttributes[keyword] = reader.GetStringOfItem());
		}
		// Legacy CK3 format had faiths nested inside religions.
		religionParser.RegisterKeyword("faiths", faithsReader => {
			var faithsParser = new Parser(implicitVariableHandling: true);
			faithsParser.RegisterRegex(CommonRegexes.String, (faithReader, faithId) => LoadFaith(faithId, faithReader));
			faithsParser.IgnoreAndLogUnregisteredItems();
			faithsParser.ParseStream(faithsReader);
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

	private static readonly string[] DetailKeywords = [
		"family",
		"graphical_faith",
		"piety_icon_group",
		"theocracy_government_type",
		"theocracy_lease_contract_type",
		"tenet_background_icon",
		"tenet_heretical_background_icon",
		"tenet_neutral_background_icon",
		"tenet_unknown_background_icon",
	];

	/// <summary>
	/// ID of the religion family, e.g. rf_abrahamic. Defined in the religion_details block in CK3 1.20+.
	/// </summary>
	public string? FamilyId => detailsAttributes.TryGetValue("family", out var family) ? family.ToString().RemQuotes() : null;

	private void LoadFaith(string faithId, BufferedReader faithReader) {
		var faithData = faithDataParser.Parse(faithReader);
		faithData.ReligionId ??= Id;

		if (faithData.InvalidatingFaithIds.Count != 0) { // Faith is an optional faith.
			foreach (var existingFaith in ReligionCollection.Faiths) {
				if (!faithData.InvalidatingFaithIds.Contains(existingFaith.Id)) {
					continue;
				}
				Logger.Debug($"Faith {faithId} is invalidated by existing {existingFaith.Id}.");
				return;
			}
			Logger.Debug($"Loading optional faith {faithId}...");
		}

		// The faith might have already been added to another religion.
		foreach (var otherReligion in ReligionCollection) {
			otherReligion.Faiths.Remove(faithId);
		}

		Faiths.AddOrReplace(new Faith(faithId, faithData, this));
	}

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