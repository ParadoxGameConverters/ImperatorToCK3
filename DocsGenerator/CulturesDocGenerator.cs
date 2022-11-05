using commonItems;
using commonItems.Collections;
using commonItems.Localization;
using ImperatorToCK3.CK3.Cultures;

namespace DocsGenerator;

public static class CulturesDocGenerator {
	private static IEnumerable<Culture> LoadCultures(string modPath) {
		Logger.Info("Loading cultures...");
		var culturesPath = Path.Combine(modPath, "common/culture/cultures");
		var files = SystemUtils.GetAllFilesInFolderRecursive(culturesPath)
			.Where(f => CommonFunctions.GetExtension(f) == "txt");

		var cultures = new IdObjectCollection<string, Culture>();
		var parser = new Parser();
		parser.RegisterRegex(CommonRegexes.String, (reader, cultureId) => {
			var culture = new Culture(cultureId, reader);
			cultures.AddOrReplace(culture);
		});
		parser.IgnoreAndLogUnregisteredItems();
		
		foreach (var relativePath in files) {
			var filePath = Path.Join(culturesPath, relativePath);
			parser.ParseFile(filePath);
		}

		return cultures;
	}

	private static string GetLocForKey(LocDB locDB, string locKey) {
		var locBlock = locDB.GetLocBlockForKey(locKey);
		if (locBlock is null) {
			Logger.Warn($"No localization found for \"{locKey}\"");
			return locKey;
		}

		var englishLoc = locBlock["english"];
		if (englishLoc is null) {
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
		if (culture.Color is not null) {
			return "#" + culture.Color.OutputHex()
				.Replace("hex", string.Empty)
				.Replace("{", string.Empty)
				.Replace("}", string.Empty)
				.Trim();
		}

		return "initial";
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
			output.WriteLine($"\t\t\t\t\t<td>{GetLocForKey(locDB, $"{culture.HeritageId}_name")}</td>");
			output.WriteLine($"\t\t\t\t\t<td>{GetLocForKey(locDB, $"{culture.EthosId}_name")}</td>");
			output.WriteLine($"\t\t\t\t\t<td>{string.Join("<br>", culture.Traditions.Select(t=>GetLocForKey(locDB, $"{t}_name")))}</td>");
			output.WriteLine($"\t\t\t\t\t<td>{GetLocForKey(locDB, $"{culture.LanguageId}_name")}</td>");
			output.WriteLine($"\t\t\t\t\t<td>{GetLocForKey(locDB, $"{culture.MartialCustomId}_name")}</td>");
			output.WriteLine("\t\t\t\t</tr>");
		}
		output.WriteLine("\t\t\t</tbody>");
		output.WriteLine("\t\t</table>");
		output.WriteLine("\t</body>");
		output.WriteLine("</html>");
		
		File.WriteAllText ("generated_docs/cultures_table.html", output.ToString());
	}
	
    public static void GenerateCulturesTable(string modPath, LocDB locDB, bool cultureColorUnderName) {
	    var cultures = LoadCultures(modPath);
	    OutputCulturesTable(cultures, locDB, cultureColorUnderName);
    }
}