﻿using System.Collections.Generic;
using System.IO;
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

			foreach (var (name, title) in titles) {
				if (title.Rank == TitleRank.kingdom && title.DeJureVassals.Count > 0) {  // title is a de jure kingdom
					var filePath = "output/" + outputModName + "/history/provinces/" + name + ".txt";
					using var historyOutput = new StreamWriter(filePath);
					foreach (var (id, province) in provinces) {
						if (title.KingdomContainsProvince(id)) {
							ProvinceOutputter.OutputProvince(historyOutput, province);
							alreadyOutputtedProvinces.Add(id);
						}
					}
				}
			}

			//create province mapping file
			var provinceMappingFilePath = "output/" + outputModName + "/history/province_mapping/province_mapping.txt";
			using var provinceMappingStream = File.OpenWrite(provinceMappingFilePath);
			using (var provinceMappingOutput = new StreamWriter(provinceMappingStream, System.Text.Encoding.UTF8)) {
				if (alreadyOutputtedProvinces.Count != provinces.Count) {
					foreach (var (id, province) in provinces) {
						if (!alreadyOutputtedProvinces.Contains(id)) {
							var baseProvID = province.BaseProvinceID;
							if (baseProvID is null) {
								Logger.Warn($"Leftover province {id} has no base province id!");
							} else {
								provinceMappingOutput.Write($"{id} = {baseProvID}");
								alreadyOutputtedProvinces.Add(id);
							}
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
