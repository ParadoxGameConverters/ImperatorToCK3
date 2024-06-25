using commonItems;
using ImperatorToCK3.CK3.Provinces;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.CommonUtils;
using Open.Collections;
using System.IO;
using System.Threading.Tasks;

namespace ImperatorToCK3.Outputter;

public static class ProvincesOutputter {
	public static async Task OutputProvinces(
		string outputModPath,
		ProvinceCollection provinces,
		Title.LandedTitles titles
	) {
		Logger.Info("Writing provinces...");
		
		// Output provinces to files named after their de jure kingdoms.
		var alreadyOutputtedProvinces = new ConcurrentHashSet<ulong>();

		var deJureKingdoms = titles.GetDeJureKingdoms();
		Parallel.ForEach(deJureKingdoms, kingdom => {
			var sb = new System.Text.StringBuilder();
			foreach (var province in provinces) {
				if (!kingdom.KingdomContainsProvince(province.Id)) {
					continue;
				}

				ProvinceOutputter.WriteProvince(sb, province);
				alreadyOutputtedProvinces.Add(province.Id);
			}
			
			var filePath = $"{outputModPath}/history/provinces/{kingdom.Id}.txt";
			using var historyOutput = new StreamWriter(filePath);
			historyOutput.Write(sb.ToString());
		});

		if (alreadyOutputtedProvinces.Count != provinces.Count) {
			var filePath = $"{outputModPath}/history/provinces/onlyDeJureDuchy.txt";
			await using var historyOutput = TextWriter.Synchronized(new StreamWriter(filePath));
			var deJureDuchies = titles.GetDeJureDuchies();
			Parallel.ForEach(deJureDuchies, duchy => {
				var sb = new System.Text.StringBuilder();
				
				foreach (var province in provinces) {
					if (alreadyOutputtedProvinces.Contains(province.Id)) {
						continue;
					}

					if (duchy.DuchyContainsProvince(province.Id)) {
						sb.AppendLine($"# {duchy.Id}");
						ProvinceOutputter.WriteProvince(sb, province);
						alreadyOutputtedProvinces.Add(province.Id);
					}
				}
				
				if (sb.Length > 0) {
					historyOutput.Write(sb.ToString());
				}
			});
		}

		// Create province mapping file.
		if (alreadyOutputtedProvinces.Count != provinces.Count) {
			var mappingsPath = $"{outputModPath}/history/province_mapping/province_mapping.txt";
			await using var mappingsWriter = FileOpeningHelper.OpenWriteWithRetries(mappingsPath, System.Text.Encoding.UTF8);
			await using var threadSafeWriter = TextWriter.Synchronized(mappingsWriter);

			foreach (var province in provinces) {
				if (alreadyOutputtedProvinces.Contains(province.Id)) {
					continue;
				}

				var baseProvId = province.BaseProvinceId;
				if (baseProvId is null) {
					continue;
				}

				await threadSafeWriter.WriteLineAsync($"{province.Id} = {baseProvId}");
				alreadyOutputtedProvinces.Add(province.Id);
			}
		}
		
		Logger.IncrementProgress();
	}
}