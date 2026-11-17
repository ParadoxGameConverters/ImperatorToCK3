using commonItems;
using commonItems.Colors;
using System;
using System.Collections.Generic;

namespace ImperatorToCK3.CK3.Religions;

/// <summary>
/// Parses faith definitions using the CK3 1.20+ format, where most attributes live in a faith_details block.
/// </summary>
internal sealed class FaithDataParser(ColorFactory colorFactory) {
	public FaithData Parse(BufferedReader faithReader) {
		var faithData = new FaithData();

		var parser = new Parser(implicitVariableHandling: true);
		parser.RegisterKeyword("faith_details", reader => ParseFaithDetails(reader, faithData));
		parser.RegisterKeyword("INVALIDATED_BY", reader => faithData.InvalidatingFaithIds = reader.GetStrings());
		parser.RegisterKeyword("main_rite", reader => faithData.MainRiteId = reader.GetString());
		parser.RegisterKeyword("holy_sites", reader => faithData.HolySiteIds.AddRange(reader.GetStrings()));
		parser.RegisterKeyword("eminent_holy_sites", reader => {
			faithData.HasEminentHolySitesBlock = true;
			faithData.EminentHolySiteIds.AddRange(reader.GetStrings());
		});
		parser.RegisterKeyword("doctrines", reader => faithData.DoctrineIds.AddRange(reader.GetStrings()));
		parser.RegisterKeyword("tenets", reader => faithData.DoctrineIds.AddRange(reader.GetStrings()));
		parser.RegisterKeyword("localization", reader => ParseLocalization(reader, faithData.Localization));
		// Generic keywords, like cultures, origin, historical names and tenet_selection_pair.
		parser.RegisterRegex(CommonRegexes.Catchall, (reader, keyword) => {
			faithData.Attributes.Add(new KeyValuePair<string, StringOfItem>(keyword, reader.GetStringOfItem()));
		});
		parser.ParseStream(faithReader);

		return faithData;
	}

	private void ParseFaithDetails(BufferedReader detailsReader, FaithData faithData) {
		var parser = new Parser(implicitVariableHandling: true);
		parser.RegisterKeyword("religion", reader => faithData.ReligionId = reader.GetString());
		parser.RegisterKeyword("color", reader => faithData.Color = ParseColor(reader));
		parser.RegisterKeyword("religious_head", reader => faithData.ReligiousHeadTitleId = ParseTitleId(reader));
		parser.RegisterKeyword("head_of_rite", reader => {
			var titleId = reader.GetString();
			if (titleId != "none") {
				faithData.HeadOfRiteTitleId = titleId;
			}
		});
		parser.RegisterRegex(CommonRegexes.Catchall, (reader, keyword) => {
			faithData.DetailsAttributes[keyword] = reader.GetStringOfItem();
		});
		parser.ParseStream(detailsReader);
	}

	private Color? ParseColor(BufferedReader reader) {
		try {
			return colorFactory.GetColor(reader);
		} catch (Exception e) {
			Logger.Warn($"Found invalid color in faith! {e.Message}");
			return null;
		}
	}

	private static string? ParseTitleId(BufferedReader reader) {
		var titleId = reader.GetString();
		return titleId == "none" ? null : titleId;
	}

	private static void ParseLocalization(BufferedReader reader, OrderedDictionary<string, StringOfItem> localization) {
		var localizationParser = new Parser(implicitVariableHandling: true);
		localizationParser.RegisterRegex(CommonRegexes.Catchall, (locReader, locKey) => {
			localization[locKey] = locReader.GetStringOfItem();
		});
		localizationParser.ParseStream(reader);
	}
}
