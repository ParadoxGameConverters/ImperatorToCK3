using commonItems;
using ImperatorToCK3.CK3.Titles;
using System.IO;

namespace ImperatorToCK3.Outputter {
	public static class TitleOutputter {
		public static void OutputTitle(StreamWriter writer, Title title, string indent) {
			writer.WriteLine($"{indent}{title.Name} = {{");

			if (title.Rank == TitleRank.barony) {
				writer.WriteLine($"{indent}\tprovince={title.Province}");
			}

			if (title.HasDefiniteForm) {
				writer.WriteLine($"{indent}\tdefinite_form=yes");
			}

			if (title.Color1 is not null) {
				writer.WriteLine($"{indent}\tcolor{title.Color1.Output()}");
			} else {
				Logger.Warn($"Title {title.Name} has no color!");
			}
			if (title.Color2 is not null) {
				writer.WriteLine($"{indent}\tcolor2{title.Color2.Output()}");
			}

			if (title.CapitalCounty is not null) {
				writer.WriteLine($"{indent}\tcapital={title.CapitalCounty.Value.Key}");
			}

			/* This line keeps the Seleucids Seleucid and not "[Dynasty]s" */
			writer.WriteLine($"{indent}\truler_uses_title_name=no");

			foreach (var vassal in title.DeJureVassals.Values) {
				OutputTitle(writer, vassal, indent + '\t');
			}

			writer.WriteLine($"{indent}}}");
		}
	}
}
