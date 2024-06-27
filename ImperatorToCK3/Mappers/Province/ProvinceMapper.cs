using commonItems;
using ImperatorToCK3.Exceptions;
using System.Collections.Generic;

namespace ImperatorToCK3.Mappers.Province;

public sealed class ProvinceMapper {
	private readonly Dictionary<ulong, IList<ulong>> imperatorToCK3ProvinceMap = new();
	private readonly Dictionary<ulong, IList<ulong>> ck3ToImperatorProvinceMap = new();

	public void LoadMappings(string mappingsPath, string mappingsVersionName) {
		Logger.Info("Loading province mappings...");

		ProvinceMappingsVersion? version = null;
		var parser = new Parser();
		parser.RegisterKeyword(mappingsVersionName, reader => version = new ProvinceMappingsVersion(reader));
		parser.IgnoreUnregisteredItems();
		parser.ParseFile(mappingsPath);

		if (version is null) {
			throw new ConverterException($"Mappings version \"{mappingsVersionName}\" not found in province mappings!");
		}
		CreateMappings(version);
		Logger.Info($"{version.Mappings.Count} mappings loaded.");
		Logger.IncrementProgress();
	}

	private void CreateMappings(ProvinceMappingsVersion mappingsVersion) {
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

	public IList<ulong> GetImperatorProvinceNumbers(ulong ck3ProvinceNumber) {
		if (ck3ToImperatorProvinceMap.TryGetValue(ck3ProvinceNumber, out var impProvs)) {
			return impProvs;
		}
		return new List<ulong>();
	}

	public IList<ulong> GetCK3ProvinceNumbers(ulong impProvinceNumber) {
		if (imperatorToCK3ProvinceMap.TryGetValue(impProvinceNumber, out var ck3Provs)) {
			return ck3Provs;
		}
		return new List<ulong>();
	}
}