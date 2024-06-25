using commonItems;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ImperatorToCK3.CK3.Wars;
using ImperatorToCK3.CommonUtils;
using System.Threading.Tasks;

namespace ImperatorToCK3.Outputter;

public static class WarsOutputter {
	public static async Task OutputWars(string outputModPath, IEnumerable<War> wars) {
		Logger.Info("Writing wars...");
		
		// Dump all into one file.
		var sb = new StringBuilder();
		foreach (var war in wars) {
			WriteWar(sb, war);
		}
		var path = Path.Combine(outputModPath, "history/wars/00_wars.txt");
		await using var output = FileOpeningHelper.OpenWriteWithRetries(path, Encoding.UTF8);
		await output.WriteAsync(sb.ToString());
		
		Logger.IncrementProgress();
	}
	private static void WriteWar(StringBuilder sb, War war) {
		sb.AppendLine("war = {");

		sb.AppendLine($"\tstart_date = {war.StartDate}");
		sb.AppendLine($"\tend_date = {war.EndDate}");
		sb.AppendLine($"\ttargeted_titles={{ {string.Join(' ', war.TargetedTitles)} }}");
		if (war.CasusBelli is not null) {
			sb.AppendLine($"\tcasus_belli = {war.CasusBelli}");
		}
		sb.AppendLine($"\tattackers={{ {string.Join(' ', war.Attackers)} }}");
		sb.AppendLine($"\tdefenders={{ {string.Join(' ', war.Defenders)} }}");
		sb.AppendLine($"\tclaimant = {war.Claimant}");

		sb.AppendLine("}");
	}
}