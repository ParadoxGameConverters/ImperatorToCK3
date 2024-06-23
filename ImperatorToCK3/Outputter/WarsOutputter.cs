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
		// dumping all into one file
		var path = Path.Combine(outputModPath, "history/wars/00_wars.txt");
		await using var output = FileOpeningHelper.OpenWriteWithRetries(path, Encoding.UTF8);
		foreach (var war in wars) {
			await OutputWar(output, war);
		}
		Logger.IncrementProgress();
	}
	private static async Task OutputWar(TextWriter output, War war) {
		await output.WriteLineAsync("war = {");

		await output.WriteLineAsync($"\tstart_date = {war.StartDate}");
		await output.WriteLineAsync($"\tend_date = {war.EndDate}");
		await output.WriteLineAsync($"\ttargeted_titles={{ {string.Join(' ', war.TargetedTitles)} }}");
		if (war.CasusBelli is not null) {
			await output.WriteLineAsync($"\tcasus_belli = {war.CasusBelli}");
		}
		await output.WriteLineAsync($"\tattackers={{ {string.Join(' ', war.Attackers)} }}");
		await output.WriteLineAsync($"\tdefenders={{ {string.Join(' ', war.Defenders)} }}");
		await output.WriteLineAsync($"\tclaimant = {war.Claimant}");

		await output.WriteLineAsync("}");
	}
}