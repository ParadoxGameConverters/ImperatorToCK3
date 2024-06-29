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

public sealed class ReligionCollection(Title.LandedTitles landedTitles) : IdObjectCollection<string, Religion> {
	private readonly Dictionary<string, OrderedSet<string>> replaceableHolySitesByFaith = [];
	public IReadOnlyDictionary<string, OrderedSet<string>> ReplaceableHolySitesByFaith => replaceableHolySitesByFaith;
	public IdObjectCollection<string, HolySite> HolySites { get; } = [];
	public IdObjectCollection<string, DoctrineCategory> DoctrineCategories { get; } = [];

	public IEnumerable<Faith> Faiths {
		get {
			return this.SelectMany(r => r.Faiths);
		}
	}

	public void LoadReligions(ModFilesystem ck3ModFS, ColorFactory colorFactory) {
		var parser = new Parser();
		parser.RegisterRegex(CommonRegexes.String, (religionReader, religionId) => {
			var religion = new Religion(religionId, religionReader, this, colorFactory);
			AddOrReplace(religion);
		});
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		parser.ParseGameFolder("common/religion/religions", ck3ModFS, "txt", recursive: true);
	}

	public void LoadConverterFaiths(string converterFaithsPath, ColorFactory colorFactory) {
		var parser = new Parser();
		parser.RegisterRegex(CommonRegexes.String, (religionReader, religionId) => {
			var optReligion = new Religion(religionId, religionReader, this, colorFactory);

			// Check if religion already exists. If it does, add converter faiths to it.
			// Otherwise, add the converter faith's religion.
			if (TryGetValue(religionId, out var religion)) {
				foreach (var faith in optReligion.Faiths) {
					faith.Religion = religion;
					religion.Faiths.Add(faith);
				}
			} else {
				Add(optReligion);
			}
		});
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		parser.ParseFile(converterFaithsPath);
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

		var missingFaithIds = new OrderedSet<string>();

		var parser = new Parser();
		parser.RegisterRegex(CommonRegexes.String, (reader, faithId) => {
			var faith = GetFaith(faithId);
			var value = reader.GetStringOfItem();
			if (faith is null) {
				missingFaithIds.Add(faithId);
				return;
			}

			var valueStr = value.ToString();
			if (value.IsArrayOrObject()) {
				replaceableHolySitesByFaith[faithId] = new OrderedSet<string>(new BufferedReader(valueStr).GetStrings());
			} else if (valueStr == "all") {
				replaceableHolySitesByFaith[faithId] = new OrderedSet<string>(faith.HolySiteIds);
			} else {
				Logger.Warn($"Unexpected value: {valueStr}");
			}
		});
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		parser.ParseFile(filePath);

		Logger.Debug($"Replaceable holy sites not loaded for missing faiths: {string.Join(", ", missingFaithIds)}");
	}

	public void LoadDoctrines(ModFilesystem ck3ModFS) {
		var parser = new Parser();
		parser.RegisterRegex(CommonRegexes.String, (reader, categoryId) =>
			DoctrineCategories.AddOrReplace(new DoctrineCategory(categoryId, reader)));
		parser.IgnoreAndLogUnregisteredItems();
		parser.ParseGameFolder("common/religion/doctrines", ck3ModFS, "txt", recursive: true);
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

		var capitalBaronyProvinceId = landedTitles[holySite.CountyId].CapitalBaronyProvinceId;
		if (capitalBaronyProvinceId is not null) {
			return landedTitles.GetBaronyForProvince((ulong)capitalBaronyProvinceId);
		}

		return null;
	}

	private static Imperator.Provinces.Province? GetImperatorProvinceForBarony(Title barony, ProvinceCollection ck3Provinces) {
		var provinceId = barony.ProvinceId;
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
			imperatorModifiers = new Dictionary<string, double>(deity.PassiveModifiers);
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
			foreach (var holySiteId in faith.HolySiteIds.ToArray()) {
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
						if (HolySites.ContainsKey(newHolySiteInSameBarony.Id)) {
							Logger.Warn($"Created duplicate holy site: {newHolySiteInSameBarony.Id}!");
						}
						HolySites.AddOrReplace(newHolySiteInSameBarony);

						faith.ReplaceHolySiteId(holySiteId, newHolySiteInSameBarony.Id);
					}
					dynamicHolySiteBaronies.Remove(holySiteBarony);
				} else if (!replaceableSiteIds.Contains(holySiteId)) {
					continue;
				} else if (dynamicHolySiteBaronies.Count != 0) {
					var selectedDynamicBarony = dynamicHolySiteBaronies[0];
					dynamicHolySiteBaronies.Remove(selectedDynamicBarony);

					var replacementSite = GenerateHolySiteForBarony(
						selectedDynamicBarony,
						faith,
						ck3Provinces,
						imperatorReligions,
						holySiteEffectMapper
					);
					if (HolySites.ContainsKey(replacementSite.Id)) {
						Logger.Warn($"Created duplicate holy site: {replacementSite.Id}!");
					}
					HolySites.AddOrReplace(replacementSite);

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
			.Select(c => c.GetFaithId(date)).ToImmutableHashSet();

		var provinceFaithIds = provinces
			.Select(p => p.GetFaithId(date)).ToImmutableHashSet();

		var aliveFaithsWithSpiritualHeadDoctrine = Faiths
			.Where(f => aliveCharacterFaithIds.Contains(f.Id) || provinceFaithIds.Contains(f.Id))
			.Where(f => f.GetDoctrineIdForDoctrineCategoryId("doctrine_head_of_faith") == "doctrine_spiritual_head")
			.ToImmutableList();

		foreach (var faith in aliveFaithsWithSpiritualHeadDoctrine) {
			GenerateReligiousHeadForFaithIfMissing(faith, titles, characters, provinces, cultures, date);
		}
	}

	private static string GetCultureIdForGeneratedHeadOfFaith(Faith faith,
		CharacterCollection characters,
		ProvinceCollection provinces,
		CultureCollection cultures,
		Date date) {
		var cultureId = provinces
			.Where(p => p.GetFaithId(date) == faith.Id)
			.Select(p => p.GetCultureId(date))
			.FirstOrDefault();
		if (cultureId is null) {
			cultureId = characters
				.Where(c => c.GetFaithId(date) == faith.Id)
				.Select(c => c.GetCultureId(date))
				.FirstOrDefault();
		}
		if (cultureId is null) {
			Logger.Warn($"Found no matching culture for religious head of {faith.Id}, using first one in database!");
			cultureId = cultures.First().Id;
		}

		return cultureId;
	}

	private void GenerateReligiousHeadForFaithIfMissing(
		Faith faith,
		Title.LandedTitles titles,
		CharacterCollection characters,
		ProvinceCollection provinces,
		CultureCollection cultures,
		Date date
	) {
		var religiousHeadTitleId = faith.ReligiousHeadTitleId;
		if (religiousHeadTitleId is null) {
			return;
		}

		if (!titles.TryGetValue(religiousHeadTitleId, out var title)) {
			Logger.Warn($"Religious head title {religiousHeadTitleId} for {faith.Id} not found!");
			return;
		}
		var holderId = title.GetHolderId(date);
		if (holderId != "0") {
			if (!characters.TryGetValue(holderId, out var holder)) {
				Logger.Warn($"Religious head {holderId} of title {title.Id} for {faith.Id} not found!");
				return;
			}

			var holderDeathDate = holder.DeathDate;
			if (holderDeathDate is null || holderDeathDate > date) {
				return;
			}
		}

		// Generate title holder.
		Logger.Debug($"Generating religious head for faith {faith.Id}...");

		// Determine culture.
		string cultureId = GetCultureIdForGeneratedHeadOfFaith(faith, characters, provinces, cultures, date);
		if (!cultures.TryGetValue(cultureId, out var culture)) {
			Logger.Warn($"Culture {cultureId} not found!");
			return;
		}

		// If title has male_names defined, use one of them for character's name.
		// Otherwise, get name from culture.
		var name = title.MaleNames?.FirstOrDefault();
		if (name is null) {
			var maleNames = culture.MaleNames.ToImmutableList();
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
		var character = new Character($"IRToCK3_head_of_faith_{faith.Id}", name, date.ChangeByYears(-age), characters);
		character.SetFaithId(faith.Id, date: null);
		character.SetCultureId(cultureId, date: null);
		var traitsToAdd = new[] {"chaste", "celibate", "devoted"};
		foreach (var traitId in traitsToAdd) {
			character.History.AddFieldValue(date: null, "traits", "trait", traitId);
		}
		characters.Add(character);
		title.SetHolder(character, date);
	}

	private List<Title> GetDynamicHolySiteBaroniesForFaith(Faith faith, IDictionary<string, ISet<Province>> provincesByFaith) {
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
			.ToArray();
		var provincesWithoutHolySite = faithTerritories.Except(provincesWithHolySite)
			.OrderByDescending(p => p.PrimaryImperatorProvince!.GetPopCount())
			.ToArray();

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
}