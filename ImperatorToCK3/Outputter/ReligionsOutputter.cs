using commonItems;
using commonItems.Serialization;
using ImperatorToCK3.CK3.Religions;
using System.IO;
using System.Linq;

namespace ImperatorToCK3.Outputter; 

public static class ReligionsOutputter {
	public static void OutputHolySites(string outputModName, ReligionCollection ck3ReligionCollection) {
		Logger.Info("Writing holy sites...");
		
		var outputPath = Path.Combine("output", outputModName, "common", "religion", "holy_sites", "IRtoCK3_sites.txt");

		using var outputStream = File.OpenWrite(outputPath);
		using var output = new StreamWriter(outputStream, System.Text.Encoding.UTF8);
		
		using var englishLocStream = File.OpenWrite(Path.Combine("output", outputModName, "localization/english/IRtoCK3_holy_sites_l_english.yml"));
		using var frenchLocStream = File.OpenWrite(Path.Combine("output", outputModName, "localization/french/IRtoCK3_holy_sites_l_french.yml"));
		using var germanLocStream = File.OpenWrite(Path.Combine("output", outputModName, "localization/german/IRtoCK3_holy_sites_l_german.yml"));
		using var russianLocStream = File.OpenWrite(Path.Combine("output", outputModName, "localization/russian/IRtoCK3_holy_sites_l_russian.yml"));
		using var simpChineseLocStream = File.OpenWrite(Path.Combine("output", outputModName, "localization/simp_chinese/IRtoCK3_holy_sites_l_simp_chinese.yml"));
		using var spanishLocStream = File.OpenWrite(Path.Combine("output", outputModName, "localization/spanish/IRtoCK3_holy_sites_l_spanish.yml"));
		using var englishLocWriter = new StreamWriter(englishLocStream, System.Text.Encoding.UTF8);
		using var frenchLocWriter = new StreamWriter(frenchLocStream, System.Text.Encoding.UTF8);
		using var germanLocWriter = new StreamWriter(germanLocStream, System.Text.Encoding.UTF8);
		using var russianLocWriter = new StreamWriter(russianLocStream, System.Text.Encoding.UTF8);
		using var simpChineseLocWriter = new StreamWriter(simpChineseLocStream, System.Text.Encoding.UTF8);
		using var spanishLocWriter = new StreamWriter(spanishLocStream, System.Text.Encoding.UTF8);

		englishLocWriter.WriteLine("l_english:");
		frenchLocWriter.WriteLine("l_french:");
		germanLocWriter.WriteLine("l_german:");
		russianLocWriter.WriteLine("l_russian:");
		simpChineseLocWriter.WriteLine("l_simp_chinese:");
		spanishLocWriter.WriteLine("l_spanish:");

		var sitesToOutput = ck3ReligionCollection.HolySites.Where(s => s.IsGeneratedByConverter)
			.ToList();

		foreach (var site in sitesToOutput) {
			output.WriteLine($"{site.Id}={PDXSerializer.Serialize(site)}");
		}
		
		// Output localization
		foreach (var site in sitesToOutput) {
			// holy site name
			var holySiteTitle = site.BaronyId ?? site.CountyId;
			if (holySiteTitle is not null) {
				string holySiteNameLocLine = $" holy_site_{site.Id}_name: \"${holySiteTitle}$\"";
				englishLocWriter.WriteLine(holySiteNameLocLine);
				frenchLocWriter.WriteLine(holySiteNameLocLine);
				germanLocWriter.WriteLine(holySiteNameLocLine);
				russianLocWriter.WriteLine(holySiteNameLocLine);
				simpChineseLocWriter.WriteLine(holySiteNameLocLine);
				spanishLocWriter.WriteLine(holySiteNameLocLine);
			} else {
				englishLocWriter.WriteLine($" holy_site_{site.Id}_name: \"Holy site\""); // fallback
			}
			
			// holy site effect name
			string holySiteEffectLocLine = $" holy_site_{site.Id}_effect_name: \"From [holy_site|E] #weak ($holy_site_{site.Id}_name$)#!\"";
			englishLocWriter.WriteLine(holySiteEffectLocLine);
			frenchLocWriter.WriteLine(holySiteEffectLocLine);
			germanLocWriter.WriteLine(holySiteEffectLocLine);
			russianLocWriter.WriteLine(holySiteEffectLocLine);
			simpChineseLocWriter.WriteLine(holySiteEffectLocLine);
			spanishLocWriter.WriteLine(holySiteEffectLocLine);
		}
	}

	public static void OutputModifiedReligions(string outputModName, ReligionCollection ck3ReligionCollection) {
		Logger.Info("Writing modified religions...");
		
		var religionsToBeOutput = ck3ReligionCollection.Where(r => r.Faiths.Any(f => f.ModifiedByConverter));
		
		var outputPath = Path.Combine("output", outputModName, "common", "religion", "religions", "zzz_IRtoCK3_modified_religions.txt");
		using var outputStream = File.OpenWrite(outputPath);
		using var output = new StreamWriter(outputStream, System.Text.Encoding.UTF8);

		foreach (var religion in religionsToBeOutput) {
			output.WriteLine($"{religion.Id}={PDXSerializer.Serialize(religion)}");
		}
	}
}