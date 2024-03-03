using ImperatorToCK3.CK3.Provinces;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.CommonUtils;
using Open.Collections;
using System.IO;
using System.Threading.Tasks;

namespace ImperatorToCK3.Outputter;

public static class ProvincesOutputter {
	public static void OutputProvinces(
		string outputModName,
		ProvinceCollection provinces,
		Title.LandedTitles titles
	) {
		// Output provinces to files named after their de jure kingdoms.
		var alreadyOutputtedProvinces = new ConcurrentHashSet<ulong>();

		var deJureKingdoms = titles.GetDeJureKingdoms();
		Parallel.ForEach(deJureKingdoms, kingdom => {
			var filePath = $"output/{outputModName}/history/provinces/{kingdom.Id}.txt";
			using var historyOutput = new StreamWriter(filePath);
			foreach (var province in provinces) {
				if (kingdom.KingdomContainsProvince(province.Id)) {
					ProvinceOutputter.OutputProvince(historyOutput, province);
					alreadyOutputtedProvinces.Add(province.Id);
				}
			}
		});

		if (alreadyOutputtedProvinces.Count != provinces.Count) {
			var filePath = $"output/{outputModName}/history/provinces/onlyDeJureDuchy.txt";
			using var historyOutput = TextWriter.Synchronized(new StreamWriter(filePath));
			var deJureDuchies = titles.GetDeJureDuchies();
			Parallel.ForEach(deJureDuchies, duchy => {
				foreach (var province in provinces) {
					if (alreadyOutputtedProvinces.Contains(province.Id)) {
						continue;
					}

					if (duchy.DuchyContainsProvince(province.Id)) {
						historyOutput.WriteLine($"# {duchy.Id}");
						ProvinceOutputter.OutputProvince(historyOutput, province);
						alreadyOutputtedProvinces.Add(province.Id);
					}
				}
			});
		}

		// Create province mapping file.
		if (alreadyOutputtedProvinces.Count != provinces.Count) {
			var mappingsPath = $"output/{outputModName}/history/province_mapping/province_mapping.txt";
			using var mappingsWriter = FileOpeningHelper.OpenWriteWithRetries(mappingsPath, System.Text.Encoding.UTF8);
			using var threadSafeWriter = TextWriter.Synchronized(mappingsWriter);

			foreach (var province in provinces) {
				if (alreadyOutputtedProvinces.Contains(province.Id)) {
					continue;
				}

				var baseProvId = province.BaseProvinceId;
				if (baseProvId is null) {
					continue;
				}

				threadSafeWriter.WriteLine($"{province.Id} = {baseProvId}");
				alreadyOutputtedProvinces.Add(province.Id);
			}
		}
	}
}