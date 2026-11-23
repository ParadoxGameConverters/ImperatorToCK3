using commonItems;
using commonItems.Colors;
using commonItems.Localization;
using commonItems.Mods;
using ImperatorToCK3.CK3.Cultures;
using System;
using System.Collections.Generic;
using System.IO;

namespace DocsGenerator;

public static class CulturesDocGenerator {
	private static IEnumerable<Culture> LoadCultures(ModFilesystem ck3ModFS, ColorFactory colorFactory, OrderedDictionary<string, bool> ck3ModFlags) {
		Logger.Info("Loading cultural pillars...");
		var pillars = new PillarCollection(colorFactory, ck3ModFlags);
		pillars.LoadPillars(ck3ModFS, ck3ModFlags);

		Logger.Info("Loading cultures...");
		var cultures = new CultureCollection(colorFactory, pillars, ck3ModFlags);
		cultures.LoadNameLists(ck3ModFS);
		cultures.LoadCultures(ck3ModFS);

		return cultures;
	}

	private static string GetLocForKey(LocDB locDB, string locKey) {
		var locBlock = locDB.GetLocBlockForKey(locKey);
		if (locBlock is null) {
			Logger.Warn($"No localization found for \"{locKey}\"");
			return locKey;
		}

		var englishLoc = locBlock["english"];
		if (string.IsNullOrEmpty(englishLoc)) {
			Logger.Warn($"No English localization found for \"{locKey}\"");
			return locKey;
		}

		// Check for nested loc.
		var dollarPos = englishLoc.IndexOf('$');
		if (dollarPos != -1) {
			var secondDollarPos = englishLoc.IndexOf('$', dollarPos + 1);
			if (secondDollarPos != -1) {
				var nesting = englishLoc.Substring(dollarPos, secondDollarPos - dollarPos + 1);
				var nestedLocKey = nesting.Trim('$');
				englishLoc = englishLoc.Replace(nesting, GetLocForKey(locDB, nestedLocKey));
			}
		}
		return englishLoc;
	}

	private static string GetCultureColorForCell(Culture culture) {
		var color = culture.Color;
		return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
	}

	private static string? GetLastAttributeId(Culture culture, string attributeKey) {
		string? attributeId = null;
		foreach (var pair in culture.Attributes) {
			if (pair.Key != attributeKey) {
				continue;
			}
			attributeId = pair.Value.ToString().RemQuotes();
		}
		return attributeId;
	}

	private static void OutputCulturesTable(IEnumerable<Culture> cultures, LocDB locDB, bool cultureColorUnderName) {
		Logger.Info("Outputting cultures table...");
		using var output = new StringWriter();

		output.WriteLine("""
		<style>
		.tg  {border-collapse:collapse;border-spacing:0;}
		.tg td{border-color:black;border-style:solid;border-width:1px;font-family:Arial, sans-serif;font-size:14px;
			overflow:hidden;padding:10px 5px;word-break:normal;text-align:left;vertical-align:center;}
		.tg th{border-color:black;border-style:solid;border-width:1px;font-family:Arial, sans-serif;font-size:14px;
			font-weight:normal;overflow:hidden;padding:10px 5px;word-break:normal;text-align:left;vertical-align:center;}
		.color-cell {
			min-width: 20px;
			text-shadow: 0 0 1px black, 0 0 1px black, 0 0 1px black, 0 0 1px black, 0 0 1px black, 0 0 1px black, 0 0 1px black, 0 0 1px black, 0 0 1px black, 0 0 1px black, 0 0 1px black, 0 0 1px black, 0 0 1px black, 0 0 1px black, 0 0 1px black, 0 0 1px black, 0 0 1px black, 0 0 1px black, 0 0 1px black, 0 0 1px black;
			color: white;
			font-weight: bold;
		}
		</style>
		""");
		output.WriteLine("<html>");
		output.WriteLine("\t<body>");
		output.WriteLine("\t\t<table class=\"tg\">");
		output.WriteLine($"""
			<thead>
				<tr>
					{(cultureColorUnderName ? "" : "<th></th>")}
					<th>Culture</th>
					<th>Heritage</th>
					<th>Ethos</th>
					<th>Traditions</th>
					<th>Language</th>
					<th>Martial custom</th>
				</tr>
			</thead>
		""");
		output.WriteLine("\t\t\t<tbody>");
		foreach (var culture in cultures) {
			output.WriteLine("\t\t\t\t<tr>");
			if (cultureColorUnderName) {
				output.WriteLine($"\t\t\t\t\t<td class=\"color-cell\" style=\"background-color: {GetCultureColorForCell(culture)}\">{GetLocForKey(locDB, culture.Id)}</td>");
			} else {
				output.WriteLine($"\t\t\t\t\t<td class=\"color-cell\" style=\"background-color: {GetCultureColorForCell(culture)}\"></td>");
				output.WriteLine($"\t\t\t\t\t<td>{GetLocForKey(locDB, culture.Id)}</td>");
			}
			output.WriteLine($"\t\t\t\t\t<td>{GetLocForKey(locDB, $"{culture.Heritage.Id}_name")}</td>");

			var ethosId = GetLastAttributeId(culture, "ethos");
			output.WriteLine($"\t\t\t\t\t<td>{(ethosId is null ? string.Empty : GetLocForKey(locDB, $"{ethosId}_name"))}</td>");

			output.WriteLine($"\t\t\t\t\t<td>{string.Join("<br>", culture.TraditionIds.Select(t => GetLocForKey(locDB, $"{t}_name")))}</td>");

			output.WriteLine($"\t\t\t\t\t<td>{GetLocForKey(locDB, $"{culture.Language.Id}_name")}</td>");

			var martialCustomId = GetLastAttributeId(culture, "martial_custom");
			output.WriteLine($"\t\t\t\t\t<td>{(martialCustomId is null ? string.Empty : GetLocForKey(locDB, $"{martialCustomId}_name"))}</td>");
			output.WriteLine("\t\t\t\t</tr>");
		}
		output.WriteLine("\t\t\t</tbody>");
		output.WriteLine("\t\t</table>");
		output.WriteLine("\t</body>");
		output.WriteLine("</html>");

		File.WriteAllText("generated_docs/cultures_table.html", output.ToString());
	}

	public static void GenerateCulturesTable(ModFilesystem ck3ModFS, ColorFactory colorFactory, LocDB locDB, OrderedDictionary<string, bool> ck3ModFlags, bool cultureColorUnderName) {
		var cultures = LoadCultures(ck3ModFS, colorFactory, ck3ModFlags);
		OutputCulturesTable(cultures, locDB, cultureColorUnderName);
	}
}
