using commonItems;
using commonItems.Serialization;
using ImperatorToCK3.CK3.Religions;
using ImperatorToCK3.CommonUtils;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ImperatorToCK3.Outputter;

public static class ReligionsOutputter {
	public static async Task OutputReligionsAndHolySites(string outputModPath, ReligionCollection ck3ReligionCollection) {
		await Task.WhenAll(
			OutputHolySites(outputModPath, ck3ReligionCollection),
			OutputReligions(outputModPath, ck3ReligionCollection)
		);
		Logger.IncrementProgress();
	}

	private static async Task OutputHolySites(string outputModPath, ReligionCollection ck3ReligionCollection) {
		Logger.Info("Writing holy sites...");

		var outputPath = Path.Combine(outputModPath, "common/religion/holy_sites/IRtoCK3_sites.txt");
		await using var output = FileOpeningHelper.OpenWriteWithRetries(outputPath, System.Text.Encoding.UTF8);

		var sitesToOutput = ck3ReligionCollection.HolySites.Where(s => s.IsGeneratedByConverter)
			.ToList();
		foreach (var site in sitesToOutput) {
			await output.WriteLineAsync($"{site.Id}={PDXSerializer.Serialize(site)}");
		}
		
		// Output localization.
		foreach (string language in ConverterGlobals.SupportedLanguages) {
			var locOutputPath = Path.Combine(outputModPath, $"localization/{language}/IRtoCK3_holy_sites_l_{language}.yml");
			await using var locWriter = FileOpeningHelper.OpenWriteWithRetries(locOutputPath, System.Text.Encoding.UTF8);
			
			await locWriter.WriteLineAsync($"l_{language}:");
			
			foreach (var site in sitesToOutput) {
				// holy site name
				var holySiteTitle = site.BaronyId ?? site.CountyId;
				if (holySiteTitle is not null) {
					string holySiteNameLocLine = $" holy_site_{site.Id}_name: \"${holySiteTitle}$\"";
					await locWriter.WriteLineAsync(holySiteNameLocLine);
				} else {
					await locWriter.WriteLineAsync($" holy_site_{site.Id}_name: \"Holy site\""); // fallback
				}

				// holy site effect name
				string holySiteEffectLocLine = $" holy_site_{site.Id}_effect_name: \"From [holy_site|E] #weak ($holy_site_{site.Id}_name$)#!\"";
				await locWriter.WriteLineAsync(holySiteEffectLocLine);
			}
		}
	}

	private static async Task OutputReligions(string outputModPath, ReligionCollection ck3ReligionCollection) {
		Logger.Info("Writing religions...");
		var outputPath = Path.Combine(outputModPath, "common/religion/religions/IRtoCK3_all_religions.txt");
		await using var output = FileOpeningHelper.OpenWriteWithRetries(outputPath, System.Text.Encoding.UTF8);

		foreach (var religion in ck3ReligionCollection) {
			await output.WriteLineAsync($"{religion.Id}={PDXSerializer.Serialize(religion)}");
		}
	}
}