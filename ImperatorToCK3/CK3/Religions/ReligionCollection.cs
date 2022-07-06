using commonItems;
using commonItems.Collections;
using commonItems.Mods;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.CK3.Provinces;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ProvinceCollection = ImperatorToCK3.CK3.Provinces.ProvinceCollection;

namespace ImperatorToCK3.CK3.Religions; 

public class ReligionCollection : IdObjectCollection<string, Religion> {
	public Dictionary<string, OrderedSet<string>> ReplaceableHolySitesByFaith { get; } = new();

	public void LoadReligions(string religionsFolderPath) {
		Logger.Info($"Loading religions from {religionsFolderPath}...");
		var files = SystemUtils.GetAllFilesInFolderRecursive(religionsFolderPath);
		foreach (var file in files) {
			var parser = new Parser();
			parser.RegisterRegex(CommonRegexes.String, (religionReader, religionId) => {
				var religion = new Religion(religionId, religionReader);
				Add(religion);
			});
			parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);

			var filePath = Path.Combine(religionsFolderPath, file);
			parser.ParseFile(filePath);
		}
	}
	public void LoadReligions(ModFilesystem ck3ModFs) {
		Logger.Info("Loading religions from CK3 game and mods...");
		const string religionsPath = "common/religion/religions";
		var files = ck3ModFs.GetAllFilesInFolderRecursive(religionsPath);
		foreach (var filePath in files) {
			var parser = new Parser();
			parser.RegisterRegex(CommonRegexes.String, (religionReader, religionId) => {
				var religion = new Religion(religionId, religionReader);
				Add(religion);
			});
			parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);

			parser.ParseFile(filePath);
		}
	}

	public void LoadReplaceableHolySites(string filePath) {
		Logger.Info("Loading replaceable holy sites...");
		
		var parser = new Parser();
		parser.RegisterRegex(CommonRegexes.String, (reader, faithId) => {
			var faith = GetFaith(faithId);
			if (faith is null) {
				Logger.Warn($"Faith \"{faithId}\" not found!");
				return;
			}

			var value = reader.GetStringOfItem();
			var valueStr = value.ToString();
			if (value.IsArrayOrObject()) {
				ReplaceableHolySitesByFaith[faithId] = new OrderedSet<string>(new BufferedReader(valueStr).GetStrings());
			} else if (valueStr == "all") {
				ReplaceableHolySitesByFaith[faithId] = new OrderedSet<string>(faith.HolySites);
			} else Logger.Warn($"Unexpected value: {valueStr}");
		});
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		parser.ParseFile(filePath);
	}

	public Faith? GetFaith(string id) {
		foreach (Religion religion in this) {
			if (religion.Faiths.TryGetValue(id, out var faith)) {
				return faith;
			}
		}

		return null;
	}

	public void DetermineHolySites(ProvinceCollection ck3Provinces, Title.LandedTitles titles) {
		var provincesByFaith = GetProvincesByFaith(ck3Provinces);
		
		foreach (var religion in this) {
			foreach (var faith in religion.Faiths) {
				var replaceableSites = ReplaceableHolySitesByFaith[faith.Id];
				var dynamicHolySiteBaronies = GetDynamicHolySiteBaroniesForFaith(faith, provincesByFaith, titles);
			}
		}
	}

	// Returns a dictionary with CK3 provinces that are mapped to Imperator provinces, grouped by faith
	public static IDictionary<string, ISet<Province>> GetProvincesByFaith(ProvinceCollection ck3Provinces) {
		var provincesByFaith = new Dictionary<string, ISet<Province>>();

		foreach (var province in ck3Provinces) {
			var imperatorProvince = province.ImperatorProvince;
			if (imperatorProvince is null) {
				continue;
			}

			var faith = province.FaithId;
			if (provincesByFaith.TryGetValue(faith, out var set)) {
				set.Add(province);
			} else {
				provincesByFaith[faith] = new HashSet<Province> {province};
			}
		}
		
		return provincesByFaith;
	}

	private static IList<Title> GetDynamicHolySiteBaroniesForFaith(Faith faith, IDictionary<string, ISet<Province>> provincesByFaith, Title.LandedTitles titles) {
		// Collect all Imperator territories that are mapped to this faith.
		ISet<Province> faithTerritories;
		if (provincesByFaith.TryGetValue(faith.Id, out var set)) {
			faithTerritories = set;
		} else {
			faithTerritories = new HashSet<Province>();
		}

		// Split the territories into 2 sets: territories that have a holy site and territories that do not.
		// Order both sets in descending order by population.
		var provincesWithHolySite = faithTerritories.Where(p => p.ImperatorProvince!.IsHolySite)
			.OrderByDescending(p=>p.ImperatorProvince!.GetPopCount())
			.ToList();
		var provincesWithoutHolySite = faithTerritories.Except(provincesWithHolySite)
			.OrderByDescending(p=>p.ImperatorProvince!.GetPopCount())
			.ToList();
		
		// Take the top 4 territories with a holy site.
		var selectedDynamicSites = provincesWithHolySite.Take(4).ToList();
		
		// Take the most populated territory without a holy site.
		var mostPopulatedProvinceWithoutHolySite = provincesWithoutHolySite.FirstOrDefault(defaultValue: null);
		if (mostPopulatedProvinceWithoutHolySite is not null) {
			selectedDynamicSites.Add(mostPopulatedProvinceWithoutHolySite);
		}

		return selectedDynamicSites
			.Select(p => titles.GetBaronyForProvince(p.Id))
			.Where(t=>t is not null)!
			.ToList<Title>();
	}
}