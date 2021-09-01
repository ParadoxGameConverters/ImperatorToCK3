using System.IO;
using ImperatorToCK3.CK3.Titles;
using commonItems;

namespace ImperatorToCK3.Outputter {
	public static class TitleOutputter {
		public static void OutputTitle(StreamWriter writer, Title title) {
			writer.WriteLine(title.Name + " = {");

			if (title.Color1 is not null) {
				writer.WriteLine("\tcolor " + title.Color1.OutputRgb());
			} else {
				Logger.Warn($"Title {title.Name} has no color.");
			}
			if (title.Color2 is not null) {
				writer.WriteLine("\tcolor2 " + title.Color2.OutputRgb());
			} else {
				Logger.Warn($"Title {title.Name} has no color2.");
			}

			if (title.CapitalCounty is not null) {
				writer.WriteLine($"\tcapital = {title.CapitalCounty.Value.Key}");
			}

			/* This line keeps the Seleucids Seleucid and not "[Dynasty]s" */
			writer.WriteLine("\truler_uses_title_name = no");

			writer.WriteLine("}");
		}
	}
}
