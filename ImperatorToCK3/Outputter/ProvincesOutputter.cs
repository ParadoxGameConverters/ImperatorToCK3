using ImperatorToCK3.CK3.Provinces;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.CommonUtils;
using System.Collections.Generic;
using System.IO;

namespace ImperatorToCK3.Outputter;
public static class ProvincesOutputter {
	public static void OutputProvinces(
		string outputModName,
		ProvinceCollection provinces,
		Title.LandedTitles titles
	) {
		// output provinces to files named after their de jure kingdoms
		var alreadyOutputtedProvinces = new HashSet<ulong>();

		var deJureKingdoms = titles.GetDeJureKingdoms();
		foreach (var kingdom in deJureKingdoms) {
			var filePath = $"output/{outputModName}/history/provinces/{kingdom.Id}.txt";
			using var historyOutput = new StreamWriter(filePath);
			foreach (var province in provinces) {
				if (kingdom.KingdomContainsProvince(province.Id)) {
					ProvinceOutputter.OutputProvince(historyOutput, province);
					alreadyOutputtedProvinces.Add(province.Id);
				}
			}
		}
		if (alreadyOutputtedProvinces.Count != provinces.Count) {
			var filePath = $"output/{outputModName}/history/provinces/onlyDeJureDuchy.txt";
			using var historyOutput = new StreamWriter(filePath);
			var deJureDuchies = titles.GetDeJureDuchies();
			foreach (var duchy in deJureDuchies) {
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
			}
		}

		// Create province mapping file.
		if (alreadyOutputtedProvinces.Count != provinces.Count) {
			var provinceMappingFilePath = $"output/{outputModName}/history/province_mapping/province_mapping.txt";
			using var provinceMappingOutput = FileOpeningHelper.OpenWriteWithRetries(provinceMappingFilePath, System.Text.Encoding.UTF8);

			foreach (var province in provinces) {
				if (alreadyOutputtedProvinces.Contains(province.Id)) {
					continue;
				}
				var baseProvId = province.BaseProvinceId;
				if (baseProvId is null) {
					continue;
				}

				provinceMappingOutput.WriteLine($"{province.Id} = {baseProvId}");
				alreadyOutputtedProvinces.Add(province.Id);
			}
		}
	}
}
