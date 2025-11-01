using commonItems;
using commonItems.Exceptions;
using ImperatorToCK3.CommonUtils.Map;
using System.Collections.Generic;
using System.Linq;

namespace ImperatorToCK3.Mappers.Province;

internal sealed class ProvinceMapper {
	private readonly Dictionary<ulong, List<ulong>> imperatorToCK3ProvinceMap = [];
	private readonly Dictionary<ulong, List<ulong>> ck3ToImperatorProvinceMap = [];

	public void LoadMappings(string mappingsPath) {
		Logger.Info("Loading province mappings...");

		ProvinceMappingsVersion? version = null;
		var parser = new Parser();
		// The converter only expects one version in a file.
		parser.RegisterRegex(CommonRegexes.String, reader => version = new ProvinceMappingsVersion(reader));
		parser.IgnoreUnregisteredItems();
		parser.ParseFile(mappingsPath);

		if (version is null) {
			throw new ConverterException($"No province mappings found in {mappingsPath}!");
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

			// We don't want many-to-many mappings.
			if (mapping.ImperatorProvinces.Count > 1 && mapping.CK3Provinces.Count > 1) {
				Logger.Warn($"Many-to-many province mapping found: {string.Join(", ", mapping.ImperatorProvinces)} -> {string.Join(", ", mapping.CK3Provinces)}");
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
		return [];
	}

	public List<ulong> GetCK3ProvinceNumbers(ulong impProvinceNumber) {
		if (imperatorToCK3ProvinceMap.TryGetValue(impProvinceNumber, out var ck3Provs)) {
			return ck3Provs;
		}
		return [];
	}

	public void DetectInvalidMappings(MapData irMapData, MapData ck3MapData) {
		Logger.Info("Detecting invalid province mappings...");

		// There should be no land-water or water-land mappings.
		foreach (var (irProvId, ck3ProvIds) in imperatorToCK3ProvinceMap) {
			bool irProvIsLand = irMapData.IsLand(irProvId);
			if (irProvIsLand) {
				ulong[] invalidTargets = [.. ck3ProvIds.Where(ck3ProvId => !ck3MapData.IsLand(ck3ProvId))];
				if (invalidTargets.Length > 0) {
					bool pluralCK3 = invalidTargets.Length > 1;
					Logger.Warn($"I:R land province {irProvId} is mapped to CK3 water province{(pluralCK3 ? "s" : "")}" +
					            $" {string.Join(',', invalidTargets)}! Fix the province mappings!");
				}
			} else {
				ulong[] invalidTargets = [.. ck3ProvIds.Where(ck3MapData.IsLand)];
				if (invalidTargets.Length > 0) {
					bool pluralCK3 = invalidTargets.Length > 1;
					Logger.Warn($"I:R water province {irProvId} is mapped to CK3 land province{(pluralCK3 ? "s" : "")}" +
					            $" {string.Join(',', invalidTargets)}! Fix the province mappings!");
				}
			}
		}
	}
}