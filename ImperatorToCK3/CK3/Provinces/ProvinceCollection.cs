using commonItems;
using commonItems.Collections;
using commonItems.Mods;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.Mappers.Culture;
using ImperatorToCK3.Mappers.Province;
using ImperatorToCK3.Mappers.Religion;
using System;
using System.Collections.Generic;
using System.IO;

namespace ImperatorToCK3.CK3.Provinces;

public class ProvinceCollection : IdObjectCollection<ulong, Province> {
	public ProvinceCollection() { }
	public ProvinceCollection(ModFilesystem ck3ModFs) {
		LoadProvinces(ck3ModFs);
	}

	private void LoadProvinces(ModFilesystem ck3ModFs) {
		var parser = new Parser();
		parser.RegisterRegex(CommonRegexes.Integer, (reader, provinceIdString) => {
			var provinceId = ulong.Parse(provinceIdString);
			var newProvince = new Province(provinceId, reader);

			if (ContainsKey(newProvince.Id)) {
				Logger.Debug($"Vanilla province duplication - {newProvince.Id} already loaded! Overwriting.");
			}
			dict[provinceId] = newProvince;
		});
		parser.IgnoreAndLogUnregisteredItems();
		
		parser.ParseGameFolder("history/provinces", ck3ModFs, "txt", recursive: true);
	}

	public void ImportVanillaProvinces(ModFilesystem ck3ModFs) {
		var existingProvinceDefinitionsCount = Count;
		Logger.Info("Importing vanilla provinces...");
		
		// Load history/provinces.
		LoadProvinces(ck3ModFs);

		// Now load the provinces that don't have unique entries in history/provinces.
		// They instead use history/province_mapping.
		foreach (var (newProvinceId, baseProvinceId) in new ProvinceMappings(ck3ModFs)) {
			if (!ContainsKey(baseProvinceId)) {
				Logger.Warn($"Base province {baseProvinceId} not found for province {newProvinceId}.");
				continue;
			}
			if (ContainsKey(newProvinceId)) {
				Logger.Debug($"Vanilla province duplication - {newProvinceId} already loaded! Preferring unique entry over mapping.");
			} else {
				var newProvince = new Province(newProvinceId, this[baseProvinceId]);
				Add(newProvince);
			}
		}
		Logger.Info($"Loaded {Count-existingProvinceDefinitionsCount} province definitions.");
			
		Logger.IncrementProgress();
	}

	public void ImportImperatorProvinces(
		Imperator.World impWorld,
		Title.LandedTitles titles,
		CultureMapper cultureMapper,
		ReligionMapper religionMapper,
		ProvinceMapper provinceMapper,
		Configuration config
	) {
		Logger.Info("Importing Imperator Provinces...");
		var counter = 0;
		// Imperator provinces map to a subset of CK3 provinces. We'll only rewrite those we are responsible for.
		foreach (var province in this) {
			var impProvinces = provinceMapper.GetImperatorProvinceNumbers(province.Id);
			// Provinces we're not affecting will not be in this list.
			if (impProvinces.Count == 0) {
				continue;
			}
			// Next, we find what province to use as its initializing source.
			var sourceProvince = DetermineProvinceSource(impProvinces, impWorld);
			if (sourceProvince is null) {
				Logger.Warn($"Could not determine source province for CK3 province {province.Id}!");
				continue; // MISMAP, or simply have mod provinces loaded we're not using.
			}
			province.InitializeFromImperator(sourceProvince.Value.Value, titles, cultureMapper, religionMapper, config);
			// And finally, initialize it.
			++counter;
		}
		Logger.Info($"{impWorld.Provinces.Count} Imperator provinces imported into {counter} CK3 provinces.");
			
		Logger.IncrementProgress();
	}

	private static KeyValuePair<ulong, Imperator.Provinces.Province>? DetermineProvinceSource(
		List<ulong> impProvinceNumbers,
		Imperator.World impWorld
	) {
		// determine ownership by province development.
		var theClaims = new Dictionary<ulong, List<Imperator.Provinces.Province>>(); // owner, offered province sources
		var theShares = new Dictionary<ulong, int>(); // owner, development                                               
		ulong? winner = null;
		long maxDev = -1;

		foreach (var imperatorProvinceId in impProvinceNumbers) {
			if (!impWorld.Provinces.TryGetValue(imperatorProvinceId, out var impProvince)) {
				Logger.Warn($"Source province {imperatorProvinceId} is not on the list of known provinces!");
				continue; // Broken mapping, or loaded a mod changing provinces without using it.
			}

			var ownerId = impProvince.OwnerCountry?.Id ?? 0;
			if (!theClaims.ContainsKey(ownerId)) {
				theClaims[ownerId] = new();
			}

			theClaims[ownerId].Add(impProvince);

			var devValue = (int)impProvince.BuildingCount + impProvince.GetPopCount();
			theShares[ownerId] = devValue;
		}
		// Let's see who the lucky winner is.
		foreach (var (owner, development) in theShares) {
			if (development > maxDev) {
				winner = owner;
				maxDev = development;
			}
		}
		if (winner is null) {
			return null;
		}

		// Now that we have a winning owner, let's find its largest province to use as a source.
		maxDev = -1; // We can have winning provinces with weight = 0;

		var toReturn = new KeyValuePair<ulong, Imperator.Provinces.Province>();
		foreach (var province in theClaims[(ulong)winner]) {
			long provinceWeight = province.BuildingCount + province.GetPopCount();

			if (provinceWeight > maxDev) {
				toReturn = new(province.Id, province);
				maxDev = provinceWeight;
			}
		}
		if (toReturn.Key == 0 || toReturn.Value is null) {
			return null;
		}
		return toReturn;
	}
}