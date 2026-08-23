using commonItems;
using commonItems.Collections;
using commonItems.Mods;
using ImperatorToCK3.CK3.Cultures;
using ImperatorToCK3.CK3.Religions;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.CommonUtils.Map;
using ImperatorToCK3.Mappers.Culture;
using ImperatorToCK3.Mappers.Province;
using ImperatorToCK3.Mappers.Religion;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ImperatorToCK3.CK3.Provinces;

internal sealed class ProvinceCollection : IdObjectCollection<ulong, Province> {
	public ProvinceCollection() { }
	public ProvinceCollection(ModFilesystem ck3ModFs) {
		LoadProvincesHistory(ck3ModFs);
	}

	private void LoadProvinceDefinitions(ProvinceDefinitions provinceDefinitions) {
		Logger.Info("Loading CK3 province definitions from map data...");

		int count = 0;
		foreach (var provinceDefinition in provinceDefinitions) {
			if (provinceDefinition.Id == 0) {
				continue;
			}

			AddOrReplace(new Province(provinceDefinition.Id));
			++count;
		}

		Logger.Debug($"Loaded {count} province definitions.");
	}

	private void LoadProvincesHistory(ModFilesystem ck3ModFs) {
		var parser = new Parser(implicitVariableHandling: true);
		parser.RegisterRegex(CommonRegexes.Integer, (reader, provinceIdString) => {
			ulong provinceId = ulong.Parse(provinceIdString);

			// If we already have history for the province, overwrite the old one with the new one.
			if (TryGetValue(provinceId, out var existingProvince)) {
				existingProvince.UpdateHistory(reader);
				return;
			}

			dict[provinceId] = new Province(provinceId, reader);
		});
		parser.IgnoreAndLogUnregisteredItems();

		parser.ParseGameFolder("history/provinces", ck3ModFs, "txt", recursive: true);
	}

	public void ImportVanillaProvinces(ModFilesystem ck3ModFs, ProvinceDefinitions provinceDefinitions, ReligionCollection religions, CultureCollection cultures) {
		var existingProvinceDefinitionsCount = Count;
		Logger.Info("Importing vanilla provinces...");

		LoadProvinceDefinitions(provinceDefinitions);
		Logger.IncrementProgress();

		// Load history/provinces.
		LoadProvincesHistory(ck3ModFs);
		Logger.IncrementProgress();

		// Cleanup: remove invalid faith and culture entries from province history
		var validFaithIds = religions.Faiths.Select(f => f.Id).ToFrozenSet();
		var validCultureIds = cultures.Select(c => c.Id).ToFrozenSet();
		foreach (var province in this) {
			var faithField = province.History.Fields["faith"];
			int removedCount = faithField.RemoveAllEntries(value => !validFaithIds.Contains(value.ToString()?.RemQuotes() ?? string.Empty));
			if (removedCount > 0) {
				Logger.Debug($"Removed {removedCount} invalid faith entries from province {province.Id}.");
			}
			
			var cultureField = province.History.Fields["culture"];
			removedCount = cultureField.RemoveAllEntries(value => !validCultureIds.Contains(value.ToString()?.RemQuotes() ?? string.Empty));
			if (removedCount > 0) {
				Logger.Debug($"Removed {removedCount} invalid culture entries from province {province.Id}.");
			}
		}

		// Now load the provinces that don't have unique entries in history/provinces.
		// They instead use history/province_mapping.
		foreach (var (newProvinceId, baseProvinceId) in new ProvinceMappings(ck3ModFs)) {
			if (!TryGetValue(baseProvinceId, out var baseProvince)) {
				Logger.Warn($"Base province {baseProvinceId} not found for province {newProvinceId}.");
				continue;
			}
			
			if (!TryGetValue(newProvinceId, out var newProvince)) {
				Logger.Debug($"Province {newProvinceId} not found.");
				continue;
			}

			newProvince.CopyEntriesFromProvince(baseProvince);
		}
		Logger.IncrementProgress();

		Logger.Info($"Loaded {Count-existingProvinceDefinitionsCount} province definitions.");
	}

	public void ImportImperatorProvinces(
		Imperator.World irWorld,
		MapData ck3MapData,
		Title.LandedTitles titles,
		CultureMapper cultureMapper,
		ReligionMapper religionMapper,
		ProvinceMapper provinceMapper,
		Date conversionDate,
		Configuration config
	) {
		Logger.Info("Importing Imperator provinces...");
		
		int importedIRProvsCount = 0;
		int modifiedCK3ProvsCount = 0;

		var provinceDefs = ck3MapData.ProvinceDefinitions;
		var landProvinces = this
			.Where(p => provinceDefs.TryGetValue(p.Id, out var def) && def.IsLand);
		
		// Imperator provinces map to a subset of CK3 provinces. We'll only rewrite those we are responsible for.
		Parallel.ForEach(landProvinces, province => {
			var sourceProvinceIds = provinceMapper.GetImperatorProvinceNumbers(province.Id);
			// Provinces we're not affecting will not be in this list.
			if (sourceProvinceIds.Count == 0) {
				return;
			}

			// Next, we find what province to use as its primary initializing source.
			var primarySource = DeterminePrimarySourceProvince(sourceProvinceIds, irWorld);
			if (primarySource is null) {
				Logger.Warn($"Could not determine primary source province for CK3 province {province.Id}!");
				return;
			}

			OrderedSet<Imperator.Provinces.Province> secondarySourceProvinces = [];
			foreach (var sourceProvinceId in sourceProvinceIds) {
				if (sourceProvinceId == primarySource.Id) {
					continue;
				}

				if (irWorld.Provinces.TryGetValue(sourceProvinceId, out var sourceProvince)) {
					secondarySourceProvinces.Add(sourceProvince);
				}
			}
			// And finally, initialize it.
			province.InitializeFromImperator(primarySource, secondarySourceProvinces, titles, cultureMapper,
				religionMapper, conversionDate, config);

			Interlocked.Add(ref importedIRProvsCount, sourceProvinceIds.Count);
			Interlocked.Increment(ref modifiedCK3ProvsCount);
		});
		Logger.Info($"{importedIRProvsCount} I:R provinces imported into {modifiedCK3ProvsCount} CK3 provinces.");

		WarnAboutCountyCapitalProvincesWithNoCultureOrReligion(titles, config.CK3BookmarkDate);

		Logger.IncrementProgress();
	}

	private void WarnAboutCountyCapitalProvincesWithNoCultureOrReligion(Title.LandedTitles titles, Date bookmarkDate) {
		// Warn about county capital provinces with no culture or religion set.
		var countyCapitalProvinceIds = titles.Counties.Select(c => c.CapitalBaronyProvinceId)
			.Where(id => id.HasValue)
			.Select(id => id!.Value);

		foreach (var provId in countyCapitalProvinceIds) {
			if (TryGetValue(provId, out var province)) {
				bool hasCulture = province.GetCultureId(bookmarkDate) is not null;
				bool hasFaith = province.GetFaithId(bookmarkDate) is not null;
				if (!hasCulture) {
					Logger.Warn($"Province {provId} is missing culture!");
				}
				if (!hasFaith) {
					Logger.Warn($"Province {provId} is missing faith!");
				}
			} else {
				Logger.Warn($"Province {provId} (county capital province) not found!");
			}
		}
	}

	public void LoadPrehistory() {
		Logger.Info("Loading provinces prehistory...");

		const string prehistoryPath = "configurables/provinces_prehistory.txt";
		var parser = new Parser(implicitVariableHandling: true);
		parser.RegisterRegex(CommonRegexes.Integer, (reader, provIdStr) => {
			var provId = ulong.Parse(provIdStr);
			
			if (TryGetValue(provId, out var province)) {
				province.UpdateHistory(reader);
			} else {
				Logger.Warn($"Province {provId} referenced in prehistory not found!");
				ParserHelpers.IgnoreItem(reader);
			}
		});
		parser.IgnoreAndLogUnregisteredItems();
		parser.ParseFile(prehistoryPath);

		Logger.IncrementProgress();
	}

	private static Imperator.Provinces.Province? DeterminePrimarySourceProvince(
		List<ulong> irProvinceIds,
		Imperator.World irWorld
	) {
		var irProvinces = new OrderedSet<Imperator.Provinces.Province>();
		foreach (var provId in irProvinceIds) {
			if (!irWorld.Provinces.TryGetValue(provId, out var irProvince)) {
				// Broken mapping, or loaded a mod changing provinces without using it.
				Logger.Warn($"Source province {provId} is not on the list of known provinces!");
				continue;
			}

			irProvinces.Add(irProvince);
		}
		
		// Determine ownership by province development.
		var theClaims = new Dictionary<ulong, OrderedSet<Imperator.Provinces.Province>>(); // owner, offered province sources
		var theShares = new Dictionary<ulong, double>(); // owner, sum of development
		foreach (var irProvince in irProvinces) {
			var ownerId = irProvince.OwnerCountry?.Id ?? 0;
			if (!theClaims.ContainsKey(ownerId)) {
				theClaims[ownerId] = new OrderedSet<Imperator.Provinces.Province>();
			}

			theClaims[ownerId].Add(irProvince);
			theShares.TryAdd(ownerId, 0);
			theShares[ownerId] += irProvince.CivilizationValue;
		}
		
		// Let's see who the lucky winner is.
		ulong? winner = null;
		double maxDev = -1;
		foreach (var (owner, development) in theShares) {
			if (development > maxDev) {
				winner = owner;
				maxDev = development;
			}
		}
		if (winner is null) {
			return null;
		}

		// Now that we have a winning owner, let's find the most developed province to use as a source.
		return theClaims[winner.Value]
			.MaxBy(p => p.CivilizationValue);
	}
}