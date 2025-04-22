using commonItems;
using commonItems.Serialization;
using ImperatorToCK3.CK3;
using ImperatorToCK3.CK3.Religions;
using ImperatorToCK3.CommonUtils;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImperatorToCK3.Outputter;

internal static class ReligionsOutputter {
	public static async Task OutputReligionsAndHolySites(string outputModPath, ReligionCollection ck3ReligionCollection, CK3LocDB ck3LocDB) {
		await Task.WhenAll(
			OutputHolySites(outputModPath, ck3ReligionCollection, ck3LocDB),
			OutputReligions(outputModPath, ck3ReligionCollection)
		);
		Logger.IncrementProgress();
	}

	private static async Task OutputHolySites(string outputModPath, ReligionCollection ck3ReligionCollection, CK3LocDB ck3LocDB) {
		Logger.Info("Writing holy sites...");

		var sitesToOutput = ck3ReligionCollection.HolySites.ToArray();
		var sb = new StringBuilder();
		foreach (var site in sitesToOutput) {
			sb.AppendLine($"{site.Id}={PDXSerializer.Serialize(site)}");
		}

		var outputPath = Path.Combine(outputModPath, "common/religion/holy_sites/all_holy_sites.txt");
		await using var output = FileHelper.OpenWriteWithRetries(outputPath, Encoding.UTF8);
		await output.WriteAsync(sb.ToString());
		sb.Clear();

		// Add localization.
		foreach (var site in ck3ReligionCollection.HolySites) {
			// holy site name
			var siteNameLocBlock = ck3LocDB.GetOrCreateLocBlock($"holy_site_{site.Id}_name");
			
			// holy site effect name
			var siteEffectLocBlock = ck3LocDB.GetOrCreateLocBlock($"holy_site_{site.Id}_effect_name");
			
			foreach (string language in ConverterGlobals.SupportedLanguages) {
				if (!siteNameLocBlock.HasLocForLanguage(language)) {
					var holySiteTitle = site.BaronyId ?? site.CountyId;
					if (holySiteTitle is not null) {
						siteNameLocBlock[language] = $"${holySiteTitle}$";
					} else {
						siteNameLocBlock[language] = "Holy site"; // fallback
					}
				}

				if (!siteEffectLocBlock.HasLocForLanguage(language)) {
					siteEffectLocBlock[language] = $"From [holy_site|E] #weak ($holy_site_{site.Id}_name$)#!";
				}
			}
		}
	}

	private static async Task OutputReligions(string outputModPath, ReligionCollection ck3ReligionCollection) {
		Logger.Info("Writing religions...");

		var sb = new StringBuilder();
		foreach (var religion in ck3ReligionCollection) {
			sb.AppendLine($"{religion.Id}={PDXSerializer.Serialize(religion)}");
		}

		var outputPath = Path.Combine(outputModPath, "common/religion/religions/IRtoCK3_all_religions.txt");
		await using var output = FileHelper.OpenWriteWithRetries(outputPath, Encoding.UTF8);
		await output.WriteAsync(sb.ToString());
	}
}