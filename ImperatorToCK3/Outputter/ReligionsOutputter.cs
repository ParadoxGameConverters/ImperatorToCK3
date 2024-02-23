using commonItems;
using commonItems.Serialization;
using ImperatorToCK3.CK3.Religions;
using ImperatorToCK3.CommonUtils;
using System.IO;
using System.Linq;

namespace ImperatorToCK3.Outputter;

public static class ReligionsOutputter {
	public static void OutputHolySites(string outputModName, ReligionCollection ck3ReligionCollection) {
		Logger.Info("Writing holy sites...");

		var outputPath = Path.Combine("output", outputModName, "common/religion/holy_sites/IRtoCK3_sites.txt");
		using var output = FileOpeningHelper.OpenWriteWithRetries(outputPath, System.Text.Encoding.UTF8);

		var sitesToOutput = ck3ReligionCollection.HolySites.Where(s => s.IsGeneratedByConverter)
			.ToList();
		foreach (var site in sitesToOutput) {
			output.WriteLine($"{site.Id}={PDXSerializer.Serialize(site)}");
		}
		
		// Output localization.
		foreach (string language in ConverterGlobals.SupportedLanguages) {
			var locOutputPath = Path.Combine("output", outputModName, $"localization/{language}/IRtoCK3_holy_sites_l_{language}.yml");
			using var locWriter = FileOpeningHelper.OpenWriteWithRetries(locOutputPath, System.Text.Encoding.UTF8);
			
			locWriter.WriteLine($"l_{language}:");
			
			foreach (var site in sitesToOutput) {
				// holy site name
				var holySiteTitle = site.BaronyId ?? site.CountyId;
				if (holySiteTitle is not null) {
					string holySiteNameLocLine = $" holy_site_{site.Id}_name: \"${holySiteTitle}$\"";
					locWriter.WriteLine(holySiteNameLocLine);
				} else {
					locWriter.WriteLine($" holy_site_{site.Id}_name: \"Holy site\""); // fallback
				}

				// holy site effect name
				string holySiteEffectLocLine = $" holy_site_{site.Id}_effect_name: \"From [holy_site|E] #weak ($holy_site_{site.Id}_name$)#!\"";
				locWriter.WriteLine(holySiteEffectLocLine);
			}
		}
	}

	public static void OutputReligions(string outputModName, ReligionCollection ck3ReligionCollection) {
		Logger.Info("Writing religions...");
		var outputPath = Path.Combine("output", outputModName, "common/religion/religions/IRtoCK3_all_religions.txt");
		using var output = FileOpeningHelper.OpenWriteWithRetries(outputPath, System.Text.Encoding.UTF8);

		foreach (var religion in ck3ReligionCollection) {
			output.WriteLine($"{religion.Id}={PDXSerializer.Serialize(religion)}");
		}
	}
}