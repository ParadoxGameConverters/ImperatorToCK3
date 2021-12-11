﻿using commonItems;
using System.Collections.Generic;
using System.IO;

namespace ImperatorToCK3.Mappers.Province {
	public class ProvinceMapper : Parser {
		private readonly Dictionary<ulong, List<ulong>> imperatorToCK3ProvinceMap = new();
		private readonly Dictionary<ulong, List<ulong>> ck3ToImperatorProvinceMap = new();
		private readonly SortedSet<ulong> validCK3Provinces = new();
		private ProvinceMappingsVersion mappingsVersion = new();

		public ProvinceMapper() {
			Logger.Info("Parsing province mappings.");
			RegisterKeys();
			ParseFile("configurables/province_mappings.txt");
			ClearRegisteredRules();
			CreateMappings();
			Logger.Info($"{mappingsVersion.Mappings.Count} mappings loaded.");
		}
		public ProvinceMapper(BufferedReader reader) {
			RegisterKeys();
			ParseStream(reader);
			ClearRegisteredRules();
			CreateMappings();
		}
		private void RegisterKeys() {
			RegisterRegex("[0-9\\.]+", reader => {
				// We support only a single, current version, so eu4-vic2 style multiple versions
				// have been cut. There should only be a single, 0.0.0.0={} block inside province_mappings.txt
				mappingsVersion = new ProvinceMappingsVersion(reader);
			});
			RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		}
		private void CreateMappings() {
			foreach (var mapping in mappingsVersion.Mappings) {
				// fix deliberate errors where we leave mappings without keys (CK2->EU4 asian wasteland comes to mind):
				if (mapping.ImperatorProvinces.Count == 0) {
					continue;
				}

				if (mapping.CK3Provinces.Count == 0) {
					continue;
				}

				foreach (var impNumber in mapping.ImperatorProvinces) {
					if (impNumber != 0) {
						imperatorToCK3ProvinceMap.Add(impNumber, mapping.CK3Provinces);
					}
				}
				foreach (var ck3Number in mapping.CK3Provinces) {
					if (ck3Number != 0) {
						ck3ToImperatorProvinceMap.Add(ck3Number, mapping.ImperatorProvinces);
					}
				}
			}
		}

		public List<ulong> GetImperatorProvinceNumbers(ulong ck3ProvinceNumber) {
			if (ck3ToImperatorProvinceMap.TryGetValue(ck3ProvinceNumber, out var impProvs)) {
				return impProvs;
			}
			return new List<ulong>();
		}

		public List<ulong> GetCK3ProvinceNumbers(ulong impProvinceNumber) {
			if (imperatorToCK3ProvinceMap.TryGetValue(impProvinceNumber, out var ck3Provs)) {
				return ck3Provs;
			}
			return new List<ulong>();
		}

		public void DetermineValidProvinces(Configuration theConfiguration) {
			Logger.Info("Loading Valid Provinces");
			var filePath = Path.Combine(theConfiguration.CK3Path, "game/map_data/definition.csv");
			using var definitionFileReader = new StreamReader(File.OpenRead(filePath));

			while (!definitionFileReader.EndOfStream) {
				var line = definitionFileReader.ReadLine();
				if (line is null || line.Length < 2) {
					continue;
				}
				var provNum = ulong.Parse(line.Substring(0, line.IndexOf(';')));
				validCK3Provinces.Add(provNum);
			}
			Logger.Info($"{validCK3Provinces.Count} valid provinces located.");
		}
	}
}
