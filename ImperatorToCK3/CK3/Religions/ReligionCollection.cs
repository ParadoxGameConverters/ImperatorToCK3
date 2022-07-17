using commonItems;
using commonItems.Collections;
using commonItems.Mods;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.CK3.Provinces;
using ImperatorToCK3.Imperator.Religions;
using ImperatorToCK3.Mappers.HolySiteEffect;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using ProvinceCollection = ImperatorToCK3.CK3.Provinces.ProvinceCollection;

namespace ImperatorToCK3.CK3.Religions;

public class ReligionCollection : IdObjectCollection<string, Religion> {
	public Dictionary<string, OrderedSet<string>> ReplaceableHolySitesByFaith { get; } = new();
	public IdObjectCollection<string, HolySite> HolySites { get; } = new();

	public IEnumerable<Faith> Faiths {
		get {
			return this.SelectMany(r => r.Faiths);
		}
	}

	private void RegisterReligionsKeywords(Parser parser) {
		parser.RegisterRegex(CommonRegexes.String, (religionReader, religionId) => {
			var religion = new Religion(religionId, religionReader);
			AddOrReplace(religion);
		});
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
	}
	public void LoadReligions(ModFilesystem ck3ModFs) {
		Logger.Info("Loading religions from CK3 game and mods...");

		var parser = new Parser();
		RegisterReligionsKeywords(parser);

		parser.ParseGameFolder("common/religion/religions", ck3ModFs, "txt", recursive: true);
	}

	private void RegisterHolySitesKeywords(Parser parser) {
		parser.RegisterRegex(CommonRegexes.String, (holySiteReader, holySiteId) => {
			HolySites.Add(new HolySite(holySiteId, holySiteReader));
		});
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
	}
	public void LoadHolySites(ModFilesystem ck3ModFs) {
		Logger.Info("Loading CK3 holy sites...");

		var parser = new Parser();
		RegisterHolySitesKeywords(parser);

		parser.ParseGameFolder("common/religion/holy_sites", ck3ModFs, "txt", recursive: true);
	}

	public void LoadReplaceableHolySites(string filePath) {
		Logger.Info("Loading replaceable holy site IDs...");
		
		var parser = new Parser();
		parser.RegisterRegex(CommonRegexes.String, (reader, faithId) => {
			var faith = GetFaith(faithId);
			var value = reader.GetStringOfItem();
			if (faith is null) {
				Logger.Warn($"Faith \"{faithId}\" not found!");
				return;
			}

			var valueStr = value.ToString();
			if (value.IsArrayOrObject()) {
				ReplaceableHolySitesByFaith[faithId] = new OrderedSet<string>(new BufferedReader(valueStr).GetStrings());
			} else if (valueStr == "all") {
				ReplaceableHolySitesByFaith[faithId] = new OrderedSet<string>(faith.HolySiteIds);
			} else {
				Logger.Warn($"Unexpected value: {valueStr}");
			}
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

	private static Title? GetHolySiteBarony(HolySite holySite, Title.LandedTitles titles) {
		if (holySite.BaronyId is not null) {
			return titles[holySite.BaronyId];
		}

		if (holySite.CountyId is null) {
			return null;
		}

		var capitalBaronyProvince = titles[holySite.CountyId].CapitalBaronyProvince;
		if (capitalBaronyProvince is not null) {
			return titles.GetBaronyForProvince((ulong)capitalBaronyProvince);
		}

		return null;
	}

	private static Imperator.Provinces.Province? GetImperatorProvinceForBarony(Title barony, ProvinceCollection ck3Provinces) {
		var provinceId = barony.Province;
		if (provinceId is null) {
			return null;
		}
		if (!ck3Provinces.TryGetValue((ulong)provinceId, out var province)) {
			return null;
		}
		return province.ImperatorProvince;
	}

	private static HolySite GenerateHolySiteForBarony(
		Title barony,
		Faith ck3Faith,
		Title.LandedTitles titles,
		ProvinceCollection ck3Provinces,
		Imperator.Religions.ReligionCollection imperatorReligions,
		HolySiteIdToDeityIdDictionary imperatorHolySiteIdToDeityIdDictionary,
		HolySiteEffectMapper holySiteEffectMapper
	) {
		var imperatorProvince = GetImperatorProvinceForBarony(barony, ck3Provinces);
		if (imperatorProvince is null) {
			Logger.Warn($"Holy site barony {barony.Id} has no associated Imperator province. Holy site generated for this barony will have no modifiers!");
			return new HolySite(barony, ck3Faith, titles);
		}

		IReadOnlyDictionary<string, double> imperatorModifiers;
		var deity = imperatorProvince.GetHolySiteDeity(imperatorHolySiteIdToDeityIdDictionary, imperatorReligions.Deities);
		if (deity is not null) {
			imperatorModifiers = deity.PassiveModifiers;
		} else {
			var religion = imperatorProvince.GetReligion(imperatorReligions);
			if (religion is not null) {
				imperatorModifiers = religion.Modifiers.ToImmutableDictionary();
			} else {
				Logger.Warn($"No Imperator religion or deity found for holy site generated in {barony} for {ck3Faith}!");
				imperatorModifiers = new Dictionary<string, double>();
			}
		}
		return new HolySite(barony, ck3Faith, titles, imperatorModifiers, holySiteEffectMapper);
	}

	public void DetermineHolySites(
		ProvinceCollection ck3Provinces,
		Title.LandedTitles titles,
		Imperator.Religions.ReligionCollection imperatorReligions,
		HolySiteIdToDeityIdDictionary imperatorHolySiteIdToDeityIdDictionary,
		HolySiteEffectMapper holySiteEffectMapper
	) {
		var provincesByFaith = GetProvincesByFaith(ck3Provinces);
		
		foreach (var faith in Faiths) {
			if (!ReplaceableHolySitesByFaith.TryGetValue(faith.Id, out var replaceableSiteIds)) {
				continue;
			}
			Logger.Info($"Determining holy sites for faith {faith.Id}...");
			
			var dynamicHolySiteBaronies = GetDynamicHolySiteBaroniesForFaith(faith, provincesByFaith, titles);
			foreach (var holySiteId in faith.HolySiteIds.ToList()) {
				if (!HolySites.TryGetValue(holySiteId, out var holySite)) {
					Logger.Warn($"Holy site with ID {holySiteId} not found!");
					continue;
				}

				var holySiteBarony = GetHolySiteBarony(holySite, titles);
				if (holySiteBarony is not null && dynamicHolySiteBaronies.Contains(holySiteBarony)) {
					// One of dynamic holy site baronies is same as an exising holy site's barony.
					// We need to avoid faith having two holy sites in one barony.
					
					if (replaceableSiteIds.Contains(holySiteId)) {
						var newHolySiteInSameBarony = GenerateHolySiteForBarony(
							holySiteBarony,
							faith,
							titles,
							ck3Provinces,
							imperatorReligions,
							imperatorHolySiteIdToDeityIdDictionary,
							holySiteEffectMapper
						);
						HolySites.Add(newHolySiteInSameBarony);

						faith.ReplaceHolySiteId(holySiteId, newHolySiteInSameBarony.Id);
					}
					dynamicHolySiteBaronies.Remove(holySiteBarony);
				} else if (!replaceableSiteIds.Contains(holySiteId)) {
					continue;
				} else if (dynamicHolySiteBaronies.Any()) {
					var selectedDynamicBarony = dynamicHolySiteBaronies[0];
					dynamicHolySiteBaronies.Remove(selectedDynamicBarony);

					var replacementSite = GenerateHolySiteForBarony(
						selectedDynamicBarony, 
						faith, 
						titles,
						ck3Provinces,
						imperatorReligions,
						imperatorHolySiteIdToDeityIdDictionary,
						holySiteEffectMapper
					);
					HolySites.Add(replacementSite);

					faith.ReplaceHolySiteId(holySiteId, replacementSite.Id);
				}
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