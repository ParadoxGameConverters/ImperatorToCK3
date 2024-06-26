using commonItems;
using commonItems.Collections;
using commonItems.Colors;
using commonItems.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ImperatorToCK3.CK3.Religions;

public sealed class Religion : IIdentifiable<string>, IPDXSerializable {
	public string Id { get; }
	public OrderedSet<string> DoctrineIds { get; } = new();

	public ReligionCollection ReligionCollection { get; }

	public Religion(string id, BufferedReader religionReader, ReligionCollection religions, ColorFactory colorFactory) {
		Id = id;
		ReligionCollection = religions;
		this.colorFactory = colorFactory;
	
		InitFaithDataParser();

		var religionParser = new Parser();
		religionParser.RegisterKeyword("doctrine", reader => DoctrineIds.Add(reader.GetString()));
		religionParser.RegisterKeyword("faiths", faithsReader => {
			var faithsParser = new Parser();
			faithsParser.RegisterRegex(CommonRegexes.String, (faithReader, faithId) => LoadFaith(faithId, faithReader));
			faithsParser.IgnoreAndLogUnregisteredItems();
			faithsParser.ParseStream(faithsReader);
		});
		religionParser.RegisterRegex(CommonRegexes.Catchall, (reader, keyword) => {
			attributes.Add(new KeyValuePair<string, StringOfItem>(keyword, reader.GetStringOfItem()));
		});
		religionParser.ParseStream(religionReader);
	}
	private void LoadFaith(string faithId, BufferedReader faithReader) {
		faithData = new FaithData();
		
		faithDataParser.ParseStream(faithReader);
		if (faithData.InvalidatingFaithIds.Any()) { // Faith is an optional faith.
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

	private void InitFaithDataParser() {
		faithDataParser.RegisterKeyword("INVALIDATED_BY", reader => {
			faithData.InvalidatingFaithIds = reader.GetStrings();
		});
		faithDataParser.RegisterKeyword("color", reader => {
			try {
				faithData.Color = colorFactory.GetColor(reader);
			} catch (Exception e) {
				Logger.Warn($"Found invalid color in faith {faithData} in religion {Id}! {e.Message}");
			}
		});
		faithDataParser.RegisterKeyword("religious_head", reader => {
			var titleId = reader.GetString();
			if (titleId != "none") {
				faithData.ReligiousHeadTitleId = titleId;
			}
		});
		faithDataParser.RegisterKeyword("holy_site", reader => faithData.HolySiteIds.Add(reader.GetString()));
		faithDataParser.RegisterKeyword("doctrine", reader => faithData.DoctrineIds.Add(reader.GetString()));
		faithDataParser.RegisterRegex(CommonRegexes.String, (reader, keyword) => {
			faithData.Attributes.Add(new KeyValuePair<string, StringOfItem>(keyword, reader.GetStringOfItem()));
		});
		faithDataParser.IgnoreAndLogUnregisteredItems();
	}

	public IdObjectCollection<string, Faith> Faiths { get; } = new();
	private readonly List<KeyValuePair<string, StringOfItem>> attributes = new();

	public string Serialize(string indent, bool withBraces) {
		var contentIndent = indent;
		if (withBraces) {
			contentIndent += '\t';
		}

		var sb = new StringBuilder();
		if (withBraces) {
			sb.AppendLine("{");
		}
		
		foreach (var doctrineId in DoctrineIds) {
			sb.Append(contentIndent).AppendLine($"doctrine={doctrineId}");
		}
		sb.AppendLine(PDXSerializer.Serialize(attributes, indent: contentIndent+'\t', withBraces: false));

		sb.Append(contentIndent).AppendLine("faiths={");
		sb.AppendLine(PDXSerializer.Serialize(Faiths, contentIndent+'\t'));
		sb.Append(contentIndent).AppendLine("}");

		if (withBraces) {
			sb.Append(indent).Append('}');
		}

		return sb.ToString();
	}

	private readonly ColorFactory colorFactory;
	private FaithData faithData = new();
	private readonly Parser faithDataParser = new();
}