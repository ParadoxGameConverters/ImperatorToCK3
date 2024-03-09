using commonItems;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ImperatorToCK3.CK3.Wars;
using ImperatorToCK3.CommonUtils;

namespace ImperatorToCK3.Outputter;

public static class WarsOutputter {
	public static void OutputWars(string outputModName, IEnumerable<War> wars) {
		Logger.Info("Writing wars...");
		// dumping all into one file
		var path = Path.Combine("output",outputModName, "history/wars/00_wars.txt");
		using var output = FileOpeningHelper.OpenWriteWithRetries(path, Encoding.UTF8);
		foreach (var war in wars) {
			OutputWar(output, war);
		}
		Logger.IncrementProgress();
	}
	private static void OutputWar(TextWriter output, War war) {
		output.WriteLine("war = {");

		output.WriteLine($"\tstart_date = {war.StartDate}");
		output.WriteLine($"\tend_date = {war.EndDate}");
		output.WriteLine($"\ttargeted_titles={{ {string.Join(' ', war.TargetedTitles)} }}");
		if (war.CasusBelli is not null) {
			output.WriteLine($"\tcasus_belli = {war.CasusBelli}");
		}
		output.WriteLine($"\tattackers={{ {string.Join(' ', war.Attackers)} }}");
		output.WriteLine($"\tdefenders={{ {string.Join(' ', war.Defenders)} }}");
		output.WriteLine($"\tclaimant = {war.Claimant}");

		output.WriteLine("}");
	}
}