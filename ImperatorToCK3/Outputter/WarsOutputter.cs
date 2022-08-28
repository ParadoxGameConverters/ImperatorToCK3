using commonItems;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ImperatorToCK3.CK3.Wars;

namespace ImperatorToCK3.Outputter; 

public static class WarsOutputter {
	public static void OutputWars(string outputModName, List<War> wars) {
		Logger.Info("Writing wars...");
		// dumping all into one file
		var path = Path.Combine("output",outputModName, "history/wars/fromImperator.txt");
		using var stream = File.OpenWrite(path);
		using var output = new StreamWriter(stream, Encoding.UTF8);
		foreach (var war in wars) {
			OutputWar(output, war);
		}
		Logger.IncrementProgress();
	}
	private static void OutputWar(StreamWriter output, War war) {
		output.WriteLine("war = {");

		output.WriteLine($"\tstart_date = {war.StartDate}");
		if (war.EndDate is not null) {
			output.WriteLine($"\tend_date = {war.EndDate}");
		}
		output.WriteLine("\ttargeted_titles={ " + string.Join(" ", war.TargetedTitles) + " }");
		if (war.CasusBelli is not null) {
			output.WriteLine($"\tcasus_belli = {war.CasusBelli}");
		}
		output.WriteLine("\tattackers={ " + string.Join(" ", war.Attackers) + " }");
		output.WriteLine("\tdefenders={ " + string.Join(" ", war.Defenders) + " }");
		output.WriteLine($"\tclaimant = {war.Claimant}");

		output.WriteLine("}");
	}
}