﻿using commonItems;
using commonItems.Collections;
using commonItems.Mods;
using ImperatorToCK3.CK3.Cultures;
using ImperatorToCK3.CK3.Religions;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.CommonUtils.Map;
using ImperatorToCK3.Exceptions;
using ImperatorToCK3.Mappers.Culture;
using ImperatorToCK3.Mappers.Province;
using ImperatorToCK3.Mappers.Religion;
using Microsoft.VisualBasic.FileIO;
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

	private void LoadProvinceDefinitions(ModFilesystem ck3ModFs) { // TODO: get rid of this (duplicates functionality of ProvinceDefinitions class)
		Logger.Info("Loading CK3 province definitions...");

		var filePath = ck3ModFs.GetActualFileLocation("map_data/definition.csv");
		if (filePath is null) {
			throw new ConverterException("Province definitions file not found!");
		}
		
		int count = 0;
		using (var parser = new TextFieldParser(filePath)) {
			parser.TextFieldType = FieldType.Delimited;
			parser.SetDelimiters(";");
			parser.CommentTokens = ["#"];
			parser.TrimWhiteSpace = true;
			
			while (!parser.EndOfData) {
				string[]? fields = parser.ReadFields();
				if (fields is null) {
					continue;
				}

				if (fields.Length < 1) {
					continue;
				}
				
				var id = ulong.Parse(fields[0]);
				if (id == 0) {
					continue;
				}

				AddOrReplace(new Province(id));
				++count;
			}
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

	public void ImportVanillaProvinces(ModFilesystem ck3ModFs, ReligionCollection religions, CultureCollection cultures) {
		var existingProvinceDefinitionsCount = Count;
		Logger.Info("Importing vanilla provinces...");

		LoadProvinceDefinitions(ck3ModFs);
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

			var secondarySourceProvinces = irWorld.Provinces
				.Where(p => sourceProvinceIds.Contains(p.Id) && p.Id != primarySource.Id)
				.ToOrderedSet();
			// And finally, initialize it.
			province.InitializeFromImperator(primarySource, secondarySourceProvinces, titles, cultureMapper,
				religionMapper, conversionDate, config);

			Interlocked.Add(ref importedIRProvsCount, sourceProvinceIds.Count);
			Interlocked.Increment(ref modifiedCK3ProvsCount);
		});
		Logger.Info($"{importedIRProvsCount} I:R provinces imported into {modifiedCK3ProvsCount} CK3 provinces.");
		
		Logger.IncrementProgress();
	}

	public void LoadPrehistory() {
		Logger.Info("Loading provinces prehistory...");

		const string prehistoryPath = "configurables/provinces_prehistory.txt";
		var parser = new Parser();
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
		IEnumerable<ulong> irProvinceIds,
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