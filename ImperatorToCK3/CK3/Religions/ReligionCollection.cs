using commonItems;
using commonItems.Collections;
using commonItems.Colors;
using commonItems.Mods;
using ImperatorToCK3.CK3.Characters;
using ImperatorToCK3.CK3.Cultures;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.CK3.Provinces;
using ImperatorToCK3.Mappers.HolySiteEffect;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using ProvinceCollection = ImperatorToCK3.CK3.Provinces.ProvinceCollection;

namespace ImperatorToCK3.CK3.Religions;

public class ReligionCollection : IdObjectCollection<string, Religion> {
	public Dictionary<string, OrderedSet<string>> ReplaceableHolySitesByFaith { get; } = new();
	public IdObjectCollection<string, HolySite> HolySites { get; } = new();
	public IdObjectCollection<string, DoctrineCategory> DoctrineCategories { get; } = new();

	public IEnumerable<Faith> Faiths {
		get {
			return this.SelectMany(r => r.Faiths);
		}
	}

	public ReligionCollection(Title.LandedTitles landedTitles) {
		this.landedTitles = landedTitles;
	}

	private void RegisterReligionsKeywords(Parser parser, ColorFactory colorFactory) {
		parser.RegisterRegex(CommonRegexes.String, (religionReader, religionId) => {
			var religion = new Religion(religionId, religionReader, this, colorFactory);
			AddOrReplace(religion);
		});
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
	}
	public void LoadReligions(ModFilesystem ck3ModFS, ColorFactory colorFactory) {
		Logger.Info("Loading religions from CK3 game and mods...");

		var parser = new Parser();
		RegisterReligionsKeywords(parser, colorFactory);

		parser.ParseGameFolder("common/religion/religions", ck3ModFS, "txt", recursive: true);

		Logger.IncrementProgress();
	}

	private void RegisterHolySitesKeywords(Parser parser) {
		parser.RegisterRegex(CommonRegexes.String, (holySiteReader, holySiteId) => {
			try {
				var holySite = new HolySite(holySiteId, holySiteReader, landedTitles);
				HolySites.AddOrReplace(holySite);
			} catch (KeyNotFoundException e) {
				Logger.Debug($"Could not add holy site {holySiteId}: {e.Message}");
			}
		});
		parser.IgnoreAndLogUnregisteredItems();
	}
	public void LoadHolySites(ModFilesystem ck3ModFS) {
		Logger.Info("Loading CK3 holy sites...");

		var parser = new Parser();
		RegisterHolySitesKeywords(parser);

		parser.ParseGameFolder("common/religion/holy_sites", ck3ModFS, "txt", recursive: true);
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

	public void LoadDoctrines(ModFilesystem ck3ModFS) {
		var parser = new Parser();
		parser.RegisterRegex(CommonRegexes.String, (reader, categoryId) => {
			DoctrineCategories.AddOrReplace(new DoctrineCategory(categoryId, reader));
		});
		parser.IgnoreAndLogUnregisteredItems();
		parser.ParseGameFolder("common/religion/doctrines", ck3ModFS, "txt", true);
	}

	public Faith? GetFaith(string id) {
		foreach (Religion religion in this) {
			if (religion.Faiths.TryGetValue(id, out var faith)) {
				return faith;
			}
		}

		return null;
	}

	private Title? GetHolySiteBarony(HolySite holySite) {
		if (holySite.BaronyId is not null) {
			return landedTitles[holySite.BaronyId];
		}

		if (holySite.CountyId is null) {
			return null;
		}

		var capitalBaronyProvince = landedTitles[holySite.CountyId].CapitalBaronyProvince;
		if (capitalBaronyProvince is not null) {
			return landedTitles.GetBaronyForProvince((ulong)capitalBaronyProvince);
		}

		return null;
	}

	private static Imperator.Provinces.Province? GetImperatorProvinceForBarony(Title barony, ProvinceCollection ck3Provinces) {
		var provinceId = barony.Province;
		if (provinceId is null) {
			return null;
		}
		if (!ck3Provinces.TryGetValue(provinceId.Value, out var province)) {
			return null;
		}
		return province.PrimaryImperatorProvince;
	}

	private HolySite GenerateHolySiteForBarony(
		Title barony,
		Faith ck3Faith,
		ProvinceCollection ck3Provinces,
		Imperator.Religions.ReligionCollection imperatorReligions,
		HolySiteEffectMapper holySiteEffectMapper
	) {
		var imperatorProvince = GetImperatorProvinceForBarony(barony, ck3Provinces);
		if (imperatorProvince is null) {
			Logger.Warn($"Holy site barony {barony.Id} has no associated Imperator province. Holy site generated for this barony will have no modifiers!");
			return new HolySite(barony, ck3Faith, landedTitles);
		}

		IReadOnlyDictionary<string, double> imperatorModifiers;
		var deity = imperatorProvince.GetHolySiteDeity(imperatorReligions);
		if (deity is not null) {
			imperatorModifiers = deity.PassiveModifiers;
		} else {
			var religion = imperatorProvince.GetReligion(imperatorReligions);
			if (religion is not null) {
				imperatorModifiers = religion.Modifiers.ToImmutableDictionary();
			} else {
				Logger.Warn($"No Imperator religion or deity found for holy site generated in {barony} for {ck3Faith.Id}!");
				imperatorModifiers = new Dictionary<string, double>();
			}
		}

		return new HolySite(barony, ck3Faith, landedTitles, imperatorModifiers, holySiteEffectMapper);
	}

	public void DetermineHolySites(
		ProvinceCollection ck3Provinces,
		Imperator.Religions.ReligionCollection imperatorReligions,
		HolySiteEffectMapper holySiteEffectMapper,
		Date date
	) {
		var provincesByFaith = GetProvincesFromImperatorByFaith(ck3Provinces, date);

		foreach (var faith in Faiths) {
			if (!ReplaceableHolySitesByFaith.TryGetValue(faith.Id, out var replaceableSiteIds)) {
				continue;
			}
			Logger.Info($"Determining holy sites for faith {faith.Id}...");

			var dynamicHolySiteBaronies = GetDynamicHolySiteBaroniesForFaith(faith, provincesByFaith);
			foreach (var holySiteId in faith.HolySiteIds.ToList()) {
				if (!HolySites.TryGetValue(holySiteId, out var holySite)) {
					Logger.Warn($"Holy site with ID {holySiteId} not found!");
					continue;
				}

				var holySiteBarony = GetHolySiteBarony(holySite);
				if (holySiteBarony is not null && dynamicHolySiteBaronies.Contains(holySiteBarony)) {
					// One of dynamic holy site baronies is same as an exising holy site's barony.
					// We need to avoid faith having two holy sites in one barony.

					if (replaceableSiteIds.Contains(holySiteId)) {
						var newHolySiteInSameBarony = GenerateHolySiteForBarony(
							holySiteBarony,
							faith,
							ck3Provinces,
							imperatorReligions,
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
						ck3Provinces,
						imperatorReligions,
						holySiteEffectMapper
					);
					HolySites.Add(replacementSite);

					faith.ReplaceHolySiteId(holySiteId, replacementSite.Id);
				}
			}
		}
	}

	// Returns a dictionary with CK3 provinces that are mapped to Imperator provinces, grouped by faith.
	public static IDictionary<string, ISet<Province>> GetProvincesFromImperatorByFaith(ProvinceCollection ck3Provinces, Date date) {
		var provincesByFaith = new Dictionary<string, ISet<Province>>();

		foreach (var province in ck3Provinces) {
			var imperatorProvince = province.PrimaryImperatorProvince;
			if (imperatorProvince is null) {
				continue;
			}

			var faith = province.GetFaithId(date);
			if (faith is null) {
				Logger.Debug($"CK3 province {province.Id} has no faith!");
				continue;
			}
			if (provincesByFaith.TryGetValue(faith, out var set)) {
				set.Add(province);
			} else {
				provincesByFaith[faith] = new HashSet<Province> {province};
			}
		}

		return provincesByFaith;
	}
	
	/// Generates religious heads for all alive faiths that have Spiritual Head doctrine and don't have a religious head.
	public void GenerateMissingReligiousHeads(
		Title.LandedTitles titles,
		CharacterCollection characters,
		ProvinceCollection provinces,
		CultureCollection cultures,
		Date date
	) {
		Logger.Info("Generating religious heads for faiths with Spiritual Head of Faith doctrine...");
		
		var aliveCharacterFaithIds = characters
			.Where(c => !c.Dead)
			.Select(c => c.FaithId).ToImmutableHashSet();
		
		var provinceFaithIds = provinces
			.Select(p => p.GetFaithId(date)).ToImmutableHashSet();
		
		var aliveFaithsWithSpiritualHeadDoctrine = Faiths
			.Where(f => aliveCharacterFaithIds.Contains(f.Id) || provinceFaithIds.Contains(f.Id))
			.Where(f => f.GetDoctrineIdForDoctrineCategoryId("doctrine_head_of_faith") == "doctrine_spiritual_head")
			.ToImmutableList();

		foreach (var faith in aliveFaithsWithSpiritualHeadDoctrine) {
			var religiousHeadTitleId = faith.ReligiousHeadTitleId;
			if (religiousHeadTitleId is null) {
				continue;
			}

			var title = titles[religiousHeadTitleId];
			var holderId = title.GetHolderId(date);
			if (holderId != "0") {
				// TODO: Check if holder is alive. WE NEED TO LOAD CK3 CHARACTERS FOR THIS!
				continue;
			}
			
			// Generate title holder.
			Logger.Debug($"Generating religious head for faith {faith.Id}...");
			// Determine culture.
			var cultureId = provinces
				.Where(p => p.GetFaithId(date) == faith.Id)
				.Select(p => p.GetCultureId(date))
				.ToImmutableList()
				.FirstOrDefault();
			if (cultureId is null) {
				cultureId = characters
					.Where(c => c.FaithId == faith.Id)
					.Select(c => c.CultureId)
					.ToImmutableList()
					.FirstOrDefault();
			}
			if (cultureId is null) {
				Logger.Warn($"Found no matching culture for religious head of {faith.Id}, using first one in database!");
				cultureId = cultures.First().Id;
			}
			var culture = cultures[cultureId];
			
			// If title has male_names defined, use one of them for character's name.
			// Otherwise, get name from culture.
			var name = title.MaleNames?.FirstOrDefault();
			if (name is null) {
				var maleNames = culture.NameList.MaleNames;
				if (maleNames.Count > 0) {
					name = maleNames.ElementAtOrDefault(Math.Abs(date.Year) % maleNames.Count);
				}
			}
			if (name is null) {
				const string fallbackName = "Alexandros";
				Logger.Warn($"Found no name for religious head of {faith.Id}, defaulting to {fallbackName}!");
				name = fallbackName;
			}
			var age = 30 + (Math.Abs(date.Year) % 50);
			var character = new Character($"IRToCK3_head_of_faith_{faith.Id}", name, date.ChangeByYears(-age)) {
				FaithId = faith.Id,
				CultureId = cultureId
			};
			var traitsToAdd = new[] {"chaste", "celibate", "devoted"};
			foreach (var traitId in traitsToAdd) {
				character.History.AddFieldValue(null, "traits", "trait", traitId);
			}
			characters.Add(character);
			title.SetHolder(character, date);
		}
	}

	private IList<Title> GetDynamicHolySiteBaroniesForFaith(Faith faith, IDictionary<string, ISet<Province>> provincesByFaith) {
		// Collect all Imperator territories that are mapped to this faith.
		ISet<Province> faithTerritories;
		if (provincesByFaith.TryGetValue(faith.Id, out var set)) {
			faithTerritories = set;
		} else {
			faithTerritories = new HashSet<Province>();
		}

		// Split the territories into 2 sets: territories that have a holy site and territories that do not.
		// Order both sets in descending order by population.
		var provincesWithHolySite = faithTerritories
			.Where(p => p.ImperatorProvinces.Any(irProv => irProv.IsHolySite))
			.OrderByDescending(p => p.PrimaryImperatorProvince!.GetPopCount())
			.ToList();
		var provincesWithoutHolySite = faithTerritories.Except(provincesWithHolySite)
			.OrderByDescending(p => p.PrimaryImperatorProvince!.GetPopCount())
			.ToList();

		// Take the top 4 territories with a holy site.
		var selectedDynamicSites = provincesWithHolySite.Take(4).ToList();

		// Take the most populated territory without a holy site.
		var mostPopulatedProvinceWithoutHolySite = provincesWithoutHolySite.FirstOrDefault(defaultValue: null);
		if (mostPopulatedProvinceWithoutHolySite is not null) {
			selectedDynamicSites.Add(mostPopulatedProvinceWithoutHolySite);
		}

		return selectedDynamicSites
			.Select(p => landedTitles.GetBaronyForProvince(p.Id))
			.Where(t=>t is not null)!
			.ToList<Title>();
	}

	private readonly Title.LandedTitles landedTitles;
}