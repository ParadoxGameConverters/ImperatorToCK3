using commonItems;
using commonItems.Collections;
using commonItems.Localization;
using ImperatorToCK3.CK3.Cultures;

namespace DocsGenerator;

public static class CulturesDocGenerator {
	private static IEnumerable<Culture> LoadCultures(string modPath) {
		var culturesPath = Path.Combine(modPath, "common/culture/cultures");
		var files = SystemUtils.GetAllFilesInFolderRecursive(culturesPath);

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
			Console.Error.WriteLine($"No localization found for \"{locKey}\"");
			return locKey;
		}

		var englishLoc = locBlock["english"];
		if (englishLoc is null) {
			Console.Error.WriteLine($"No English localization found for \"{locKey}\"");
			return locKey;
		}
		return englishLoc;
	}

	private static void OutputCulturesTable(IEnumerable<Culture> cultures, LocDB locDB) {
		using var stream = File.OpenWrite("cultures_table.html");
		using var output = new StreamWriter(stream, System.Text.Encoding.UTF8);

		output.WriteLine("""
			<style type="text/css">
			.tg  {border-collapse:collapse;border-spacing:0;}
			.tg td{border-color:black;border-style:solid;border-width:1px;font-family:Arial, sans-serif;font-size:14px;
				overflow:hidden;padding:10px 5px;word-break:normal;text-align:left;vertical-align:top;}
			.tg th{border-color:black;border-style:solid;border-width:1px;font-family:Arial, sans-serif;font-size:14px;
				font-weight:normal;overflow:hidden;padding:10px 5px;word-break:normal;text-align:left;vertical-align:top;}

			</style>
		""");
		output.WriteLine("<html>");
		output.WriteLine("\t<body>");
		output.WriteLine("\t\t<table class=\"tg\">");
		output.WriteLine("""
			<thead>
				<tr>
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
			output.WriteLine($"\t\t\t\t<td>{GetLocForKey(locDB, culture.Id)}</td>");
			output.WriteLine($"\t\t\t\t<td>{GetLocForKey(locDB, culture.HeritageId)}</td>");
			output.WriteLine($"\t\t\t\t<td>{GetLocForKey(locDB, culture.EthosId)}</td>");
			output.WriteLine($"\t\t\t\t<td>{string.Join("<br>", culture.Traditions.Select(t=>GetLocForKey(locDB, t)))}</td>");
			output.WriteLine($"\t\t\t\t<td>{GetLocForKey(locDB, culture.LanguageId)}</td>");
			output.WriteLine($"\t\t\t\t<td>{GetLocForKey(locDB, culture.MartialCustomId)}</td>");
			output.WriteLine("\t\t\t\t</tr>");
		}
		output.WriteLine("\t\t\t</tbody>");
		output.WriteLine("\t\t</table>");
		output.WriteLine("\t</body>");
		output.WriteLine("</html>");
	}
	
    public static void GenerateCulturesTable(string modPath) {
	    var cultures = LoadCultures(modPath);
	    OutputCulturesTable(cultures);
    }
}