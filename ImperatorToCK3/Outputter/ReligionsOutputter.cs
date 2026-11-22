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
		CreateReligionFolders(outputModPath);

		await Task.WhenAll(
			OutputHolySites(outputModPath, ck3ReligionCollection, ck3LocDB),
			OutputReligions(outputModPath, ck3ReligionCollection),
			OutputFaiths(outputModPath, ck3ReligionCollection),
			OutputRites(outputModPath, ck3ReligionCollection),
			OutputFaithHistory(outputModPath, ck3ReligionCollection)
		);
		Logger.IncrementProgress();
	}

	private static void CreateReligionFolders(string outputModPath) {
		foreach (var folder in new[] { "holy_site_types", "religion_types", "faith_types", "rite_types" }) {
			SystemUtils.TryCreateFolder(Path.Combine(outputModPath, "common", "religion", folder));
		}
		SystemUtils.TryCreateFolder(Path.Combine(outputModPath, "history", "faiths"));
	}

	private static async Task OutputHolySites(string outputModPath, ReligionCollection ck3ReligionCollection, CK3LocDB ck3LocDB) {
		Logger.Info("Writing holy sites...");

		var sitesToOutput = ck3ReligionCollection.HolySites.ToArray();
		var sb = new StringBuilder();
		foreach (var site in sitesToOutput) {
			sb.AppendLine($"{site.Id}={PDXSerializer.Serialize(site)}");
		}

		var outputPath = Path.Combine(outputModPath, "common/religion/holy_site_types/all_holy_sites.txt");
		await using var output = FileHelper.OpenWriteWithRetries(outputPath, Encoding.UTF8);
		await output.WriteAsync(sb.ToString());
		sb.Clear();

		// Add localization.
		foreach (var site in ck3ReligionCollection.HolySites) {
			// holy site name
			var siteNameLocBlock = ck3LocDB.GetOrCreateLocBlock($"holy_site_{site.Id}_name");
			
			// holy site effect description
			var siteEffectLocBlock = ck3LocDB.GetOrCreateLocBlock($"holy_site_{site.Id}_effects");
			// CK3 1.20+ renamed the effect loc key from _effect_name to _effects.
			// Reuse existing translations if the new key has no localization yet.
			var legacyEffectLocKey = $"holy_site_{site.Id}_effect_name";
			ck3LocDB.TryGetValue(legacyEffectLocKey, out var legacyEffectLocBlock);
			
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
					if (legacyEffectLocBlock is not null && legacyEffectLocBlock.HasLocForLanguage(language)) {
						siteEffectLocBlock[language] = $"${legacyEffectLocKey}$";
					} else {
						siteEffectLocBlock[language] = $"From [holy_site|E] #weak ($holy_site_{site.Id}_name$)#!";
					}
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

		var outputPath = Path.Combine(outputModPath, "common/religion/religion_types/IRtoCK3_all_religions.txt");
		await using var output = FileHelper.OpenWriteWithRetries(outputPath, Encoding.UTF8);
		await output.WriteAsync(sb.ToString());
	}

	private static async Task OutputFaiths(string outputModPath, ReligionCollection ck3ReligionCollection) {
		Logger.Info("Writing faiths...");

		var sb = new StringBuilder();
		foreach (var faith in ck3ReligionCollection.Faiths) {
			sb.AppendLine($"{faith.Id}={PDXSerializer.Serialize(faith)}");
		}

		var outputPath = Path.Combine(outputModPath, "common/religion/faith_types/IRtoCK3_all_faiths.txt");
		await using var output = FileHelper.OpenWriteWithRetries(outputPath, Encoding.UTF8);
		await output.WriteAsync(sb.ToString());
	}

	private static async Task OutputRites(string outputModPath, ReligionCollection ck3ReligionCollection) {
		var ritesToOutput = ck3ReligionCollection.Rites.ToArray();
		if (ritesToOutput.Length == 0) {
			return;
		}

		Logger.Info("Writing rites...");

		var sb = new StringBuilder();
		foreach (var rite in ritesToOutput) {
			sb.AppendLine($"{rite.Id}={PDXSerializer.Serialize(rite)}");
		}

		var outputPath = Path.Combine(outputModPath, "common/religion/rite_types/IRtoCK3_all_rites.txt");
		await using var output = FileHelper.OpenWriteWithRetries(outputPath, Encoding.UTF8);
		await output.WriteAsync(sb.ToString());
	}

	private static async Task OutputFaithHistory(string outputModPath, ReligionCollection ck3ReligionCollection) {
		if (ck3ReligionCollection.ConverterFaithIds.Count == 0) {
			return;
		}

		Logger.Info("Writing faith history...");

		var sb = new StringBuilder();
		foreach (var faithId in ck3ReligionCollection.ConverterFaithIds) {
			sb.AppendLine($"{faithId}={{");
			sb.AppendLine("\t1.1.1={");
			// In CK3 1.20+ faiths with no characters or counties count as non-created.
			sb.AppendLine("\t\tcreated = yes");
			sb.AppendLine("\t}");
			sb.AppendLine("}");
		}

		var outputPath = Path.Combine(outputModPath, "history/faiths/IRtoCK3_converter_faiths.txt");
		await using var output = FileHelper.OpenWriteWithRetries(outputPath, Encoding.UTF8);
		await output.WriteAsync(sb.ToString());
	}
}