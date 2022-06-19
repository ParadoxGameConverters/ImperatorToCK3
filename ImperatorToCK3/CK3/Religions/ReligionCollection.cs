using commonItems;
using commonItems.Collections;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.CK3.Provinces;
using ImperatorToCK3.Mappers.Province;
using ImperatorToCK3.Mappers.Religion;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ProvinceCollection = ImperatorToCK3.CK3.Provinces.ProvinceCollection;

namespace ImperatorToCK3.CK3.Religions; 

public class ReligionCollection {
	public Dictionary<string, OrderedSet<Religion>> ReligionsPerFile { get; } = new();
	public Dictionary<string, OrderedSet<string>> ReplaceableHolySitesByFaith { get; } = new();
	
	public void LoadReligions(string religionsFolderPath) {
		var files = SystemUtils.GetAllFilesInFolderRecursive(religionsFolderPath);
		foreach (var file in files) {
			var religionsInFile = new OrderedSet<Religion>();
			
			var parser = new Parser();
			parser.RegisterRegex(CommonRegexes.String, (religionReader, religionId) => {
				var religion = new Religion(religionId, religionReader);
				religionsInFile.Add(religion);
			});
			parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);

			var filePath = Path.Combine(religionsFolderPath, file);
			parser.ParseFile(filePath);

			ReligionsPerFile[file] = religionsInFile;
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
		foreach (Religion religion in ReligionsPerFile.Values.SelectMany(religionSet => religionSet)) {
			if (religion.Faiths.TryGetValue(id, out var faith)) {
				return faith;
			}
		}

		return null;
	}

	public void DetermineHolySites() {
		foreach (var religionsSet in ReligionsPerFile.Values) {
			foreach (var religion in religionsSet) {
				foreach (var faith in religion.Faiths) {
					
				}
			}
		}
	}

	// Returns CK3 a dictionary with CK3 provinces that are mapped to Imperator provinces, grouped by faith
	public IDictionary<string, ISet<Province>> GetProvincesByFaith(ProvinceCollection ck3Provinces) {
		var provincesByFaith = new Dictionary<string, ISet<Province>>();

		foreach (var province in ck3Provinces) {
			var imperatorProvince = province.ImperatorProvince;
			if (imperatorProvince is null) {
				continue;
			}

			var faith = province.Religion;
			if (provincesByFaith.TryGetValue(faith, out var set)) {
				set.Add(province);
			} else {
				provincesByFaith[faith] = new HashSet<Province> {province};
			}
		}
		
		return provincesByFaith;
	}

	private IList<Title> GetDynamicHolySiteBaroniesForFaith(Faith faith, IDictionary<string, ISet<Province>> provincesByFaith, Title.LandedTitles titles) {
		var baroniesToReturn = new List<Title>();
		
		// Collect all Imperator territories that are mapped to this faith.
		ISet<Province> faithTerritories;
		if (provincesByFaith.TryGetValue(faith.Id, out var set)) {
			faithTerritories = set;
		} else {
			faithTerritories = new HashSet<Province>();
		}

		// Split the territories into 2 sets: territories that have a holy site and territories that do not.
		// Order both sets in descending order by population.
		var provincesWithHolySite = faithTerritories.Where(p => p.ImperatorProvince!.HolySite)
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