using commonItems;
using commonItems.Collections;
using commonItems.Mods;
using CsvHelper;
using CsvHelper.Configuration;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.Exceptions;
using ImperatorToCK3.Mappers.Culture;
using ImperatorToCK3.Mappers.Province;
using ImperatorToCK3.Mappers.Religion;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace ImperatorToCK3.CK3.Provinces;

public class ProvinceCollection : IdObjectCollection<ulong, Province> {
	public ProvinceCollection() { }
	public ProvinceCollection(ModFilesystem ck3ModFs) {
		LoadProvincesHistory(ck3ModFs);
	}

	private void LoadProvinceDefinitions(ModFilesystem ck3ModFs) {
		Logger.Info("Loading CK3 province definitions...");

		var filePath = ck3ModFs.GetActualFileLocation("map_data/definition.csv");
		if (filePath is null) {
			throw new ConverterException("Province definitions file not found!");
		}
		
		var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture) {
			Delimiter = ";", HasHeaderRecord = false, AllowComments = true
		};
		var provinceDefinition = new {
			Id = default(ulong),
		};
		using var reader = new StreamReader(filePath);
		using var csv = new CsvReader(reader, csvConfig);
		var records = csv.GetRecords(provinceDefinition);

		var count = 0;
		foreach (var record in records) {
			var id = record.Id;
			if (id == 0) {
				continue;
			}
			
			AddOrReplace(new Province(id));
			++count;
		}
		
		Logger.Debug($"Loaded {count} province definitions.");
	}

	private void LoadProvincesHistory(ModFilesystem ck3ModFs) {
		var parser = new Parser();
		parser.RegisterRegex(CommonRegexes.Integer, (reader, provinceIdString) => {
			var provinceId = ulong.Parse(provinceIdString);
			var newProvince = new Province(provinceId, reader);
			dict[provinceId] = newProvince;
		});
		parser.IgnoreAndLogUnregisteredItems();
		
		parser.ParseGameFolder("history/provinces", ck3ModFs, "txt", recursive: true);
	}

	public void ImportVanillaProvinces(ModFilesystem ck3ModFs) {
		var existingProvinceDefinitionsCount = Count;
		Logger.Info("Importing vanilla provinces...");
		
		LoadProvinceDefinitions(ck3ModFs);
		Logger.IncrementProgress();
		
		// Load history/provinces.
		LoadProvincesHistory(ck3ModFs);
		Logger.IncrementProgress();

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
		Logger.IncrementProgress();
		
		Logger.Info($"Loaded {Count-existingProvinceDefinitionsCount} province definitions.");
	}

	public void ImportImperatorProvinces(
		Imperator.World irWorld,
		Title.LandedTitles titles,
		CultureMapper cultureMapper,
		ReligionMapper religionMapper,
		ProvinceMapper provinceMapper,
		Configuration config
	) {
		Logger.Info("Importing Imperator provinces...");
		var importedIRProvsCount = 0;
		var modifiedCK3ProvsCount = 0;
		// Imperator provinces map to a subset of CK3 provinces. We'll only rewrite those we are responsible for.
		foreach (var province in this) {
			var sourceProvinceIds = provinceMapper.GetImperatorProvinceNumbers(province.Id);
			// Provinces we're not affecting will not be in this list.
			if (sourceProvinceIds.Count == 0) {
				continue;
			}
			// Next, we find what province to use as its primary initializing source.
			var primarySource = DeterminePrimarySourceProvince(sourceProvinceIds, irWorld);
			if (primarySource is null) {
				Logger.Warn($"Could not determine primary source province for CK3 province {province.Id}!");
				continue;
			}
			var secondarySourceProvinces = irWorld.Provinces
				.Where(p => sourceProvinceIds.Contains(p.Id) && p.Id != primarySource.Id)
				.ToOrderedSet();
			// And finally, initialize it.
			province.InitializeFromImperator(primarySource, secondarySourceProvinces, titles, cultureMapper, religionMapper, config);
			
			importedIRProvsCount += sourceProvinceIds.Count;
			++modifiedCK3ProvsCount;
		}
		Logger.Info($"{importedIRProvsCount} I:R provinces imported into {modifiedCK3ProvsCount} CK3 provinces.");
			
		Logger.IncrementProgress();
	}

	public void LoadPrehistory() {
		Logger.Info("Loading provinces prehistory...");
		
		const string prehistoryPath = "configurables/provinces_prehistory.txt";
		var parser = new Parser();
		parser.RegisterRegex(CommonRegexes.Integer, (reader, provIdStr) => {
			var provId = ulong.Parse(provIdStr);
			this[provId].UpdateHistory(reader);
		});
		parser.IgnoreAndLogUnregisteredItems();
		parser.ParseFile(prehistoryPath);
		
		Logger.IncrementProgress();
	}

	private static Imperator.Provinces.Province? DeterminePrimarySourceProvince(
		List<ulong> impProvinceNumbers,
		Imperator.World irWorld
	) {
		// determine ownership by province development.
		var theClaims = new Dictionary<ulong, List<Imperator.Provinces.Province>>(); // owner, offered province sources
		var theShares = new Dictionary<ulong, int>(); // owner, development                                               
		ulong? winner = null;
		long maxDev = -1;

		foreach (var imperatorProvinceId in impProvinceNumbers) {
			if (!irWorld.Provinces.TryGetValue(imperatorProvinceId, out var impProvince)) {
				Logger.Warn($"Source province {imperatorProvinceId} is not on the list of known provinces!");
				continue; // Broken mapping, or loaded a mod changing provinces without using it.
			}

			var ownerId = impProvince.OwnerCountry?.Id ?? 0;
			if (!theClaims.ContainsKey(ownerId)) {
				theClaims[ownerId] = new List<Imperator.Provinces.Province>();
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
		maxDev = -1; // We can have winning provinces with weight = 0.

		Imperator.Provinces.Province? provinceToReturn = null;
		foreach (var province in theClaims[winner.Value]) {
			long provinceWeight = province.BuildingCount + province.GetPopCount();

			if (provinceWeight > maxDev) {
				provinceToReturn = province;
				maxDev = provinceWeight;
			}
		}

		return provinceToReturn;
	}
}