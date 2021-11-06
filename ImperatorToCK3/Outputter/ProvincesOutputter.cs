using System.Collections.Generic;
using System.IO;
using System.Linq;
using ImperatorToCK3.CK3.Provinces;
using ImperatorToCK3.CK3.Titles;
using commonItems;

namespace ImperatorToCK3.Outputter {
	public static class ProvincesOutputter {
		public static void OutputProvinces(
			string outputModName,
			Dictionary<ulong, Province> provinces,
			Dictionary<string, Title> titles
		) {
			// output provinces to files named after their de jure kingdoms
			var alreadyOutputtedProvinces = new HashSet<ulong>();

			var deJureKingdoms = titles.Values.Where(
				t => t.Rank == TitleRank.kingdom && t.DeJureVassals.Count > 0
			);
			foreach (var kingdom in deJureKingdoms) {
				var filePath = $"output/{outputModName}/history/provinces/{kingdom.Name}.txt";
				using var historyOutput = new StreamWriter(filePath);
				foreach (var (id, province) in provinces) {
					if (kingdom.KingdomContainsProvince(id)) {
						ProvinceOutputter.OutputProvince(historyOutput, province);
						alreadyOutputtedProvinces.Add(id);
					}
				}
			}
			if (alreadyOutputtedProvinces.Count != provinces.Count) {
				var filePath = $"output/{outputModName}/history/provinces/onlyDeJureDuchy.txt";
				using var historyOutput = new StreamWriter(filePath);
				var deJureDuchies = titles.Values.Where(
					t => t.Rank == TitleRank.duchy && t.DeJureVassals.Count > 0
				);
				foreach (var duchy in deJureDuchies) {
					foreach (var (id, province) in provinces) {
						if (alreadyOutputtedProvinces.Contains(id)) {
							continue;
						}
						if (duchy.DuchyContainsProvince(id)) {
							historyOutput.WriteLine($"# {duchy.Name}");
							ProvinceOutputter.OutputProvince(historyOutput, province);
							alreadyOutputtedProvinces.Add(id);
						}
					}
				}
			}

			//create province mapping file
			var provinceMappingFilePath = $"output/{outputModName}/history/province_mapping/province_mapping.txt";
			using var provinceMappingStream = File.OpenWrite(provinceMappingFilePath);
			using (var provinceMappingOutput = new StreamWriter(provinceMappingStream, System.Text.Encoding.UTF8)) {
				if (alreadyOutputtedProvinces.Count != provinces.Count) {
					foreach (var (id, province) in provinces) {
						if (alreadyOutputtedProvinces.Contains(id)) {
							continue;
						}
						var baseProvId = province.BaseProvinceId;
						if (baseProvId is null) {
							Logger.Warn($"Leftover province {id} has no base province id!");
						} else {
							provinceMappingOutput.WriteLine($"{id} = {baseProvId}");
							alreadyOutputtedProvinces.Add(id);
						}
					}
				}
			}

			if (alreadyOutputtedProvinces.Count != provinces.Count) {
				Logger.Error("Not all provinces were outputted!");
			}
		}
	}
}
