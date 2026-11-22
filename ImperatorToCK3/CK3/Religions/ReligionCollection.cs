using commonItems;
using commonItems.Collections;
using commonItems.Colors;
using commonItems.Mods;
using DotLiquid;
using ImperatorToCK3.CK3.Characters;
using ImperatorToCK3.CK3.Cultures;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.CK3.Provinces;
using ImperatorToCK3.CommonUtils;
using ImperatorToCK3.Mappers.HolySiteEffect;
using Open.Collections;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using ProvinceCollection = ImperatorToCK3.CK3.Provinces.ProvinceCollection;

namespace ImperatorToCK3.CK3.Religions;

internal sealed class ReligionCollection(Title.LandedTitles landedTitles) : IdObjectCollection<string, Religion> {
	private readonly Dictionary<string, OrderedSet<string>> replaceableHolySitesByFaith = [];
	private readonly Dictionary<string, string?> defaultRiteByFaithCache = new(StringComparer.Ordinal);
	private Dictionary<string, Faith>? faithCache;
	public IReadOnlyDictionary<string, OrderedSet<string>> ReplaceableHolySitesByFaith => replaceableHolySitesByFaith;
	public IdObjectCollection<string, HolySite> HolySites { get; } = [];
	public IdObjectCollection<string, DoctrineGroup> DoctrineGroups { get; } = [];
	public IdObjectCollection<string, DoctrineType> DoctrineTypes { get; } = [];
	public IdObjectCollection<string, Rite> Rites { get; } = [];

	/// <summary>
	/// IDs of CK3 tenet types. In CK3 1.20+ tenets are defined separately from doctrines.
	/// </summary>
	public OrderedSet<string> TenetIds { get; } = [];

	/// <summary>
	/// IDs of faiths defined by the converter. In CK3 1.20+ faiths with no characters or counties
	/// count as non-created, so the converter marks its faiths as created in faith history.
	/// </summary>
	public OrderedSet<string> ConverterFaithIds { get; } = [];

	public IEnumerable<Faith> Faiths {
		get {
			return this.SelectMany(r => r.Faiths);
		}
	}

	public bool IsTenet(string id) {
		if (TenetIds.Count > 0) {
			return TenetIds.Contains(id);
		}
		// Tenet types not loaded (e.g. in unit tests). Fall back to the naming convention used by CK3.
		return id.StartsWith("tenet_", StringComparison.Ordinal);
	}

	/// <summary>
	/// CK3 1.20+ history files use rite instead of faith. Reads the separate rite history field
	/// and adds matching faith entries, so the rest of the converter can keep working with faith IDs.
	/// The rite entries are kept intact for output.
	/// </summary>
	public void ResolveRiteIdsInFaithHistory(History history) {
		if (Rites.Count == 0
			|| !history.Fields.TryGetValue("faith", out var faithField)
			|| !history.Fields.TryGetValue("rite", out var riteField)) {
			return;
		}

		RemoveInvalidRiteEntries(riteField);
		AddFaithEntriesFromRites(riteField.InitialEntries, faithField, date: null);
		foreach (var (date, entries) in riteField.DateToEntriesDict) {
			AddFaithEntriesFromRites(entries, faithField, date);
		}
	}

	private void AddFaithEntriesFromRites(List<KeyValuePair<string, object>> riteEntries, IHistoryField faithField, Date? date) {
		foreach (var (_, value) in riteEntries) {
			var riteId = GetHistoryId(value);
			if (riteId is null || !Rites.TryGetValue(riteId, out var rite) || rite.FaithId is null) {
				continue;
			}

			AddFaithEntryIfMissing(faithField, date, rite.FaithId);
		}
	}

	private static void AddFaithEntryIfMissing(IHistoryField faithField, Date? date, string faithId) {
		List<KeyValuePair<string, object>>? entries = date is null
			? faithField.InitialEntries
			: faithField.DateToEntriesDict.TryGetValue(date.Value, out var datedEntries) ? datedEntries : null;

		if (entries is not null) {
			foreach (var (setter, value) in entries) {
				if (setter is "faith" or "religion" && GetHistoryId(value) == faithId) {
					return;
				}
			}
		}

		faithField.AddEntryToHistory(date, "faith", HistoryFactory.GetValue(faithId));
	}

	private void RemoveInvalidRiteEntries(IHistoryField riteField) {
		int removed = riteField.RemoveAllEntries(value => {
			var riteId = GetHistoryId(value);
			return riteId is null || !Rites.ContainsKey(riteId);
		});
		if (removed > 0) {
			Logger.Debug($"Removed {removed} invalid rite entries from history.");
		}

		// Empty date blocks would otherwise shadow earlier valid entries when reading history values.
		var emptyDates = riteField.DateToEntriesDict
			.Where(kvp => kvp.Value.Count == 0)
			.Select(kvp => kvp.Key)
			.ToArray();
		foreach (var date in emptyDates) {
			riteField.DateToEntriesDict.Remove(date);
		}
	}

	/// <summary>
	/// Converts faith entries in a history to rite entries for output. Existing matching rites are kept;
	/// otherwise the faith's main rite (or first rite for the faith) is used.
	/// </summary>
	public void ConvertFaithHistoryToRites(History history) {
		if (!history.Fields.TryGetValue("faith", out var faithField)
			|| !history.Fields.TryGetValue("rite", out var riteField)) {
			return;
		}

		RemoveInvalidRiteEntries(riteField);

		if (faithField.InitialEntries.Count > 0) {
			var faithId = GetHistoryId(faithField.InitialEntries[^1].Value);
			if (faithId is not null) {
				var riteId = GetMatchingRiteId(riteField, date: null, faithId) ?? GetDefaultRiteIdForFaith(faithId);
				if (riteId is not null) {
					faithField.InitialEntries.RemoveAll(_ => true);
					SetRiteEntry(riteField, date: null, riteId);
				}
			}
		}

		foreach (var (date, faithEntries) in faithField.DateToEntriesDict.ToArray()) {
			if (faithEntries.Count == 0) {
				continue;
			}

			var faithId = GetHistoryId(faithEntries[^1].Value);
			if (faithId is null) {
				continue;
			}

			var riteId = GetMatchingRiteId(riteField, date, faithId) ?? GetDefaultRiteIdForFaith(faithId);
			if (riteId is null) {
				continue;
			}

			faithField.DateToEntriesDict.Remove(date);
			SetRiteEntry(riteField, date, riteId);
		}
	}

	public string? GetDefaultRiteIdForFaith(string faithId) {
		if (defaultRiteByFaithCache.TryGetValue(faithId, out var cachedRiteId)) {
			return cachedRiteId;
		}

		string? riteId = null;
		if (GetFaith(faithId) is { } faith) {
			var mainRiteId = faith.MainRiteId;
			// A rite can be shared by multiple faiths (for example roman_rite is the main rite
			// of both christian_faith and catholic), so don't require rite.FaithId to match.
			if (mainRiteId is not null && Rites.ContainsKey(mainRiteId)) {
				riteId = mainRiteId;
			} else {
				foreach (var rite in Rites) {
					if (rite.FaithId == faithId) {
						riteId = rite.Id;
						break;
					}
				}
			}
		}

		if (riteId is null) {
			Logger.Warn($"No rite found for faith {faithId}, cannot output rite history!");
		}

		defaultRiteByFaithCache[faithId] = riteId;
		return riteId;
	}

	private string? GetMatchingRiteId(IHistoryField riteField, Date? date, string faithId) {
		List<KeyValuePair<string, object>>? entries = date is null
			? riteField.InitialEntries
			: riteField.DateToEntriesDict.TryGetValue(date.Value, out var datedEntries) ? datedEntries : null;
		if (entries is null) {
			return null;
		}

		for (int i = entries.Count - 1; i >= 0; --i) {
			var riteId = GetHistoryId(entries[i].Value);
			if (riteId is not null && Rites.TryGetValue(riteId, out var rite) && rite.FaithId == faithId) {
				return riteId;
			}
		}

		return null;
	}

	private static void SetRiteEntry(IHistoryField riteField, Date? date, string riteId) {
		if (date is null) {
			riteField.InitialEntries.RemoveAll(_ => true);
		} else {
			riteField.DateToEntriesDict.Remove(date.Value);
		}
		riteField.AddEntryToHistory(date, "rite", HistoryFactory.GetValue(riteId));
	}

	private static string? GetHistoryId(object value) => value.ToString()?.RemQuotes();

	public void LoadReligions(ModFilesystem ck3ModFS, ColorFactory colorFactory) {
		faithCache = null;
		defaultRiteByFaithCache.Clear();

		Logger.Info("Loading CK3 religions...");
		var parser = new Parser(implicitVariableHandling: true);
		parser.RegisterRegex(CommonRegexes.String, (religionReader, religionId) => {
			var religion = new Religion(religionId, religionReader, this);
			if (religion.FamilyId is null) {
				Logger.Warn($"Religion {religionId} has no family defined in religion_details!");
			}
			AddOrReplace(religion);
		});
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		parser.ParseGameFolder("common/religion/religion_types", ck3ModFS, "txt", recursive: true);

		// In CK3 1.20+ faiths have their own folder and reference their religion in faith_details.
		LoadFaiths(ck3ModFS, colorFactory);
		// Rites are the lowest level of the religious hierarchy in CK3 1.20+.
		LoadRites(ck3ModFS, colorFactory);
	}

	public void LoadFaiths(ModFilesystem ck3ModFS, ColorFactory colorFactory) {
		faithCache = null;
		defaultRiteByFaithCache.Clear();

		Logger.Info("Loading CK3 faiths...");
		var faithDataParser = new FaithDataParser(colorFactory);
		var parser = new Parser(implicitVariableHandling: true);
		parser.RegisterRegex(CommonRegexes.String, (faithReader, faithId) => {
			var faithData = faithDataParser.Parse(faithReader);
			AddOrReplaceFaith(faithId, faithData, isFromConverter: false);
		});
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		parser.ParseGameFolder("common/religion/faith_types", ck3ModFS, "txt", recursive: true);
	}

	public void LoadRites(ModFilesystem ck3ModFS, ColorFactory colorFactory) {
		faithCache = null;
		defaultRiteByFaithCache.Clear();

		Logger.Info("Loading CK3 rites...");
		var parser = new Parser(implicitVariableHandling: true);
		parser.RegisterRegex(CommonRegexes.String, (riteReader, riteId) => {
			Rites.AddOrReplace(new Rite(riteId, riteReader, this, colorFactory));
		});
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		parser.ParseGameFolder("common/religion/rite_types", ck3ModFS, "txt", recursive: true);
	}

	private Faith? AddOrReplaceFaith(string faithId, FaithData faithData, bool isFromConverter) {
		if (faithData.InvalidatingFaithIds.Count != 0) { // Faith is an optional faith.
			foreach (var existingFaith in Faiths) {
				if (!faithData.InvalidatingFaithIds.Contains(existingFaith.Id)) {
					continue;
				}
				Logger.Debug($"Faith {faithId} is invalidated by existing {existingFaith.Id}.");
				return null;
			}
			Logger.Debug($"Loading optional faith {faithId}...");
		}

		var religionId = faithData.ReligionId;
		if (religionId is null) {
			Logger.Warn($"Faith {faithId} has no religion defined in faith_details!");
			return null;
		}
		if (!TryGetValue(religionId, out var religion)) {
			Logger.Warn($"Religion {religionId} of faith {faithId} not found!");
			return null;
		}

		// The faith might have already been added to another religion.
		foreach (var otherReligion in this) {
			if (!ReferenceEquals(otherReligion, religion)) {
				otherReligion.Faiths.Remove(faithId);
			}
		}

		var faith = new Faith(faithId, faithData, religion, isFromConverter);
		religion.Faiths.AddOrReplace(faith);
		faithCache = null;
		return faith;
	}

	public void LoadConverterReligions(string converterReligionsPath, ColorFactory colorFactory, Hash liquidVariables) {
		if (!File.Exists(converterReligionsPath)) {
			return;
		}

		Logger.Info("Loading converter religions...");
		faithCache = null;
		var parser = new Parser(implicitVariableHandling: true);
		parser.RegisterRegex(CommonRegexes.String, (religionReader, religionId) => {
			if (ContainsKey(religionId)) {
				Logger.Debug($"Religion {religionId} already exists, ignoring the converter's definition.");
				return;
			}
			var religion = new Religion(religionId, religionReader, this);
			if (religion.FamilyId is null) {
				Logger.Warn($"Converter religion {religionId} has no family defined in religion_details!");
			}
			Add(religion);
		});
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		parser.ParseLiquidFile(converterReligionsPath, liquidVariables);
	}

	public void LoadConverterFaiths(string converterFaithsPath, ColorFactory colorFactory, Hash liquidVariables) {
		faithCache = null;
		defaultRiteByFaithCache.Clear();
		OrderedSet<Faith> loadedConverterFaiths = [];
		var faithDataParser = new FaithDataParser(colorFactory);

		var parser = new Parser(implicitVariableHandling: true);
		parser.RegisterRegex(CommonRegexes.String, (faithReader, faithId) => {
			var faithData = faithDataParser.Parse(faithReader);
			var faith = AddOrReplaceFaith(faithId, faithData, isFromConverter: true);
			if (faith is null) {
				return;
			}
			loadedConverterFaiths.Add(faith);
			ConverterFaithIds.Add(faith.Id);
		});
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		parser.ParseLiquidFile(converterFaithsPath, liquidVariables);
		faithCache = null;

		// Create mainline rites for converter faiths that don't have a scripted rite.
		GenerateMainlineRitesForConverterFaiths(loadedConverterFaiths);

		// Validation: every faith should have a pilgrimage doctrine.
		ValidatePilgrimageDoctrine(loadedConverterFaiths);

		// Validation: every faith should have a funeral doctrine.
		ValidateFuneralDoctrine(loadedConverterFaiths);

		// Validation: every faith should have a coronation doctrine.
		ValidateCoronationDoctrine(loadedConverterFaiths);

		// Validation: every faith should have a theism doctrine.
		ValidateTheismDoctrine(loadedConverterFaiths);
	}

	public void LoadConverterRites(string converterRitesPath, ColorFactory colorFactory, Hash liquidVariables) {
		if (!File.Exists(converterRitesPath)) {
			return;
		}

		faithCache = null;
		defaultRiteByFaithCache.Clear();
		Logger.Info("Loading converter rites...");
		var parser = new Parser(implicitVariableHandling: true);
		parser.RegisterRegex(CommonRegexes.String, (riteReader, riteId) => {
			Rites.AddOrReplace(new Rite(riteId, riteReader, this, colorFactory));
		});
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		parser.ParseLiquidFile(converterRitesPath, liquidVariables);
	}

	private void GenerateMainlineRitesForConverterFaiths(IEnumerable<Faith> converterFaiths) {
		foreach (var faith in converterFaiths) {
			if (faith.MainRiteId is not null) {
				continue;
			}

			if (!Rites.ContainsKey(faith.Id)) {
				// Without a scripted rite, the game creates a dynamic rite with a key that cannot be referenced.
				Rites.AddOrReplace(Rite.CreateMainlineRite(faith));
			}
			faith.MainRiteId = faith.Id;
		}
	}

	private void ValidateTheismDoctrine(OrderedSet<Faith> loadedConverterFaiths)
	{
		string? theismFallback = DoctrineGroups.TryGetValue("doctrine_theism", out var theismCategory)
			? theismCategory.DoctrineIds.FirstOrDefault(d => d == "doctrine_polytheist")
			: null;
		foreach (var converterFaith in loadedConverterFaiths) {
			var theismDoctrine = converterFaith.GetDoctrineIdsForDoctrineGroupId("doctrine_theism");
			if (theismDoctrine.Count == 0) {
				if (theismFallback is not null) {
					Logger.Warn($"Faith {converterFaith.Id} has no theism doctrine! Setting {theismFallback}");
					converterFaith.DoctrineIds.Add(theismFallback);
				} else {
					Logger.Warn($"Faith {converterFaith.Id} has no theism doctrine!");
				}
			}
		}
	}

	private void ValidateCoronationDoctrine(OrderedSet<Faith> loadedConverterFaiths)
	{
		string? coronationFallback = DoctrineGroups.TryGetValue("doctrine_coronation", out var coronationCategory)
			? coronationCategory.DoctrineIds.FirstOrDefault(d => d == "doctrine_no_anointment")
			: null;
		foreach (var converterFaith in loadedConverterFaiths) {
			var coronationDoctrine = converterFaith.GetDoctrineIdsForDoctrineGroupId("doctrine_coronation");
			if (coronationDoctrine.Count == 0) {
				if (coronationFallback is not null) {
					Logger.Warn($"Faith {converterFaith.Id} has no coronation doctrine! Setting {coronationFallback}");
					converterFaith.DoctrineIds.Add(coronationFallback);
				} else {
					Logger.Warn($"Faith {converterFaith.Id} has no coronation doctrine!");
				}
			}
		}
	}

	private void ValidateFuneralDoctrine(OrderedSet<Faith> loadedConverterFaiths)
	{
		string? funeralFallback = DoctrineGroups.TryGetValue("doctrine_funeral", out var funeralCategory)
			? funeralCategory.DoctrineIds.FirstOrDefault(d => d == "doctrine_funeral_stoic")
			: null;
		foreach (var converterFaith in loadedConverterFaiths) {
			var funeralDoctrine = converterFaith.GetDoctrineIdsForDoctrineGroupId("doctrine_funeral");
			if (funeralDoctrine.Count == 0) {
				if (funeralFallback is not null) {
					Logger.Warn($"Faith {converterFaith.Id} has no funeral doctrine! Setting {funeralFallback}");
					converterFaith.DoctrineIds.Add(funeralFallback);
				} else {
					Logger.Warn($"Faith {converterFaith.Id} has no funeral doctrine!");
				}
			}
		}
	}

	private void ValidatePilgrimageDoctrine(OrderedSet<Faith> loadedConverterFaiths)
	{
		string? pilgrimageFallback = DoctrineGroups.TryGetValue("doctrine_pilgrimage", out var pilgrimageCategory)
			? pilgrimageCategory.DoctrineIds.FirstOrDefault(d => d == "doctrine_pilgrimage_encouraged")
			: null;
		foreach (var converterFaith in loadedConverterFaiths) {
			var pilgrimageDoctrine = converterFaith.GetDoctrineIdsForDoctrineGroupId("doctrine_pilgrimage");
			if (pilgrimageDoctrine.Count == 0) {
				if (pilgrimageFallback is not null) {
					Logger.Warn($"Faith {converterFaith.Id} has no pilgrimage doctrine! Setting {pilgrimageFallback}");
					converterFaith.DoctrineIds.Add(pilgrimageFallback);
				} else {
					Logger.Warn($"Faith {converterFaith.Id} has no pilgrimage doctrine!");
				}
			}
		}
	}

	public void RemoveChristianAndIslamicSyncretismFromAllFaiths() {
		Logger.Info("Removing Christian and Islamic syncretism tenets from all faiths...");
		string[] tenetsToRemove = ["tenet_christian_syncretism", "tenet_islamic_syncretism"];
		
		foreach (var religion in this) {
			religion.DoctrineIds.Remove(tenetsToRemove);
		}
		foreach (var faith in Faiths) {
			faith.DoctrineIds.Remove(tenetsToRemove);
		}
		// In CK3 1.20+ tenets are defined on rites.
		foreach (var rite in Rites) {
			rite.DoctrineIds.Remove(tenetsToRemove);
		}
	}

	private void RegisterHolySitesKeywords(Parser parser, bool areSitesFromConverter) {
		parser.RegisterRegex(CommonRegexes.String, (holySiteReader, holySiteId) => {
			try {
				var holySite = new HolySite(holySiteId, holySiteReader, landedTitles, areSitesFromConverter);
				HolySites.AddOrReplace(holySite);
			} catch (KeyNotFoundException e) {
				Logger.Debug($"Could not add holy site {holySiteId}: {e.Message}");
			}
		});
		parser.IgnoreAndLogUnregisteredItems();
	}
	public void LoadHolySites(ModFilesystem ck3ModFS) {
		Logger.Info("Loading CK3 holy sites...");

		var parser = new Parser(implicitVariableHandling: true);
		RegisterHolySitesKeywords(parser, areSitesFromConverter: false);

		parser.ParseGameFolder("common/religion/holy_site_types", ck3ModFS, "txt", recursive: true);
	}
	public void LoadConverterHolySites(string converterHolySitesPath) {
		Logger.Info("Loading converter holy sites...");

		var parser = new Parser(implicitVariableHandling: true);
		RegisterHolySitesKeywords(parser, areSitesFromConverter: true);

		parser.ParseFile(converterHolySitesPath);
	}

	public void LoadReplaceableHolySites(string filePath) {
		Logger.Info("Loading replaceable holy site IDs...");

		var missingFaithIds = new OrderedSet<string>();

		var parser = new Parser(implicitVariableHandling: true);
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

		if (missingFaithIds.Count > 0) {
			Logger.Debug($"Replaceable holy sites not loaded for missing faiths: {string.Join(", ", missingFaithIds)}");
		}
	}

	public void LoadDoctrines(ModFilesystem ck3ModFS) {
		// Doctrine groups define what categories doctrines belong to.
		var groupParser = new Parser(implicitVariableHandling: true);
		groupParser.RegisterRegex(CommonRegexes.String, (reader, groupId) =>
			DoctrineGroups.AddOrReplace(new DoctrineGroup(groupId, reader)));
		groupParser.IgnoreAndLogUnregisteredItems();
		groupParser.ParseGameFolder("common/religion/doctrine_group_types", ck3ModFS, "txt", recursive: true);

		// In CK3 1.20+ doctrines declare their group with doctrine_group_type.
		var doctrineParser = new Parser(implicitVariableHandling: true);
		doctrineParser.RegisterRegex(CommonRegexes.String, (reader, doctrineId) => {
			var doctrineType = new DoctrineType(doctrineId, reader);
			DoctrineTypes.AddOrReplace(doctrineType);

			if (doctrineType.GroupId is null) {
				Logger.Debug($"Doctrine {doctrineId} has no doctrine_group_type!");
				return;
			}
			if (DoctrineGroups.TryGetValue(doctrineType.GroupId, out var group)) {
				group.AddDoctrineId(doctrineId);
			} else {
				Logger.Debug($"Doctrine group {doctrineType.GroupId} of doctrine {doctrineId} not found!");
			}
		});
		doctrineParser.IgnoreAndLogUnregisteredItems();
		doctrineParser.ParseGameFolder("common/religion/doctrine_types", ck3ModFS, "txt", recursive: true);

		// In CK3 1.20+ tenets are defined separately from doctrines.
		var tenetParser = new Parser(implicitVariableHandling: true);
		tenetParser.RegisterRegex(CommonRegexes.String, (reader, tenetId) => {
			TenetIds.Add(tenetId);
			ParserHelpers.IgnoreItem(reader);
		});
		tenetParser.IgnoreAndLogUnregisteredItems();
		tenetParser.ParseGameFolder("common/religion/tenet_types", ck3ModFS, "txt", recursive: true);

		Logger.Info($"Loaded {DoctrineGroups.Count} doctrine groups, {DoctrineTypes.Count} doctrines and {TenetIds.Count} tenets.");
	}

	public Faith? GetFaith(string id) {
		if (faithCache is null) {
			faithCache = [];
			foreach (var religion in this) {
				foreach (var faith in religion.Faiths) {
					faithCache[faith.Id] = faith;
				}
			}
		}

		return faithCache.TryGetValue(id, out var result) ? result : null;
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

		System.Collections.Generic.OrderedDictionary<string, double> imperatorModifiers;
		var deity = imperatorProvince.GetHolySiteDeity(imperatorReligions);
		if (deity is not null) {
			imperatorModifiers = new(deity.PassiveModifiers);
		} else {
			var religion = imperatorProvince.GetReligion(imperatorReligions);
			if (religion is not null) {
				imperatorModifiers = new(religion.Modifiers);
			} else {
				Logger.Warn($"No Imperator religion or deity found for holy site generated in {barony} for {ck3Faith.Id}!");
				imperatorModifiers = new();
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
	public static Dictionary<string, HashSet<Province>> GetProvincesFromImperatorByFaith(ProvinceCollection ck3Provinces, Date date) {
		var provincesByFaith = new Dictionary<string, HashSet<Province>>();

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
				provincesByFaith[faith] = [province];
			}
		}

		return provincesByFaith;
	}

	/// <summary>
	/// Generates religious heads for all alive faiths that have Spiritual Head doctrine and don't have a religious head.
	/// </summary>
	public void GenerateMissingReligiousHeads(
		Title.LandedTitles titles,
		CharacterCollection characters,
		ProvinceCollection provinces,
		CultureCollection cultures,
		Date date
	) {
		Logger.Info("Generating religious heads for faiths with Spiritual Head of Faith doctrine...");

		var aliveCharacterFaithIds = characters
			.Where(c => c.DeathDate is null || c.DeathDate > date)
			.Select(c => c.GetFaithId(date)).ToImmutableHashSet();

		var provinceFaithIds = provinces
			.Select(p => p.GetFaithId(date)).ToImmutableHashSet();

		var aliveFaithsWithSpiritualHeadDoctrine = new List<Faith>();
		foreach (var faith in Faiths) {
			if (!(aliveCharacterFaithIds.Contains(faith.Id) || provinceFaithIds.Contains(faith.Id))) {
				continue;
			}
			if (!faith.GetDoctrineIdsForDoctrineGroupId("doctrine_head_of_faith").Contains("doctrine_spiritual_head")) {
				continue;
			}
			aliveFaithsWithSpiritualHeadDoctrine.Add(faith);
		}
		
		// Don't generate religious heads for Christianity before it was founded.
		Date startOfChristianityInCK3 = "30.1.1"; // Based on first holder in k_papal_state history.
		if (date < startOfChristianityInCK3) {
			aliveFaithsWithSpiritualHeadDoctrine.RemoveAll(f => f.Religion.Id == "christianity_religion");
		}

		foreach (var faith in aliveFaithsWithSpiritualHeadDoctrine) {
			GenerateReligiousHeadForFaithIfMissing(faith, titles, characters, provinces, cultures, date);
		}
	}

	private static string GetCultureIdForGeneratedHeadOfFaith(Faith faith,
		CharacterCollection characters,
		ProvinceCollection provinces,
		Title.LandedTitles titles,
		CultureCollection cultures,
		Date date) {
		var cultureId = provinces
			.Where(p => p.GetFaithId(date) == faith.Id)
			.Select(p => p.GetCultureId(date))
			.FirstOrDefault();
		if (cultureId is null) {
			cultureId = characters
				.Where(c => c.BirthDate <= date && (c.DeathDate is null || c.DeathDate > date))
				.Where(c => c.GetFaithId(date) == faith.Id)
				.Select(c => c.GetCultureId(date))
				.FirstOrDefault();
		}
		if (cultureId is null && faith.HeadTitleId is not null) {
			if (titles.TryGetValue(faith.HeadTitleId, out var title)) {
				var capitalCounty = title.CapitalCounty;
				var capitalProvince = capitalCounty?.CapitalBaronyProvinceId;
				if (capitalProvince is not null) {
					cultureId = provinces[capitalProvince.Value].GetCultureId(date);
				}
			}
		}
		if (cultureId is null) {
			Logger.Warn($"Found no matching culture for religious head of {faith.Id}, using first one in database!");
			cultureId = cultures.First().Id;
		}

		return cultureId;
	}

	private static void GenerateReligiousHeadForFaithIfMissing(
		Faith faith,
		Title.LandedTitles titles,
		CharacterCollection characters,
		ProvinceCollection provinces,
		CultureCollection cultures,
		Date date
	) {
		var religiousHeadTitleId = faith.HeadTitleId;
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
		string cultureId = GetCultureIdForGeneratedHeadOfFaith(faith, characters, provinces, titles, cultures, date);
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

	private List<Title> GetDynamicHolySiteBaroniesForFaith(Faith faith, Dictionary<string, HashSet<Province>> provincesByFaith) {
		// Collect all Imperator territories that are mapped to this faith.
		if (!provincesByFaith.TryGetValue(faith.Id, out var faithTerritories)) {
			return [];
		}

		// Single-pass partition: split territories into holy-site and non-holy-site lists.
		List<Province> withHolySite = [];
		Province? bestWithout = null;
		int bestWithoutPop = -1;
		foreach (var p in faithTerritories) {
			if (p.ImperatorProvinces.Any(irProv => irProv.IsHolySite)) {
				withHolySite.Add(p);
			} else {
				var pop = p.PrimaryImperatorProvince!.GetPopCount();
				if (pop > bestWithoutPop) {
					bestWithoutPop = pop;
					bestWithout = p;
				}
			}
		}

		// Take the top 4 territories with a holy site (sort only the holy-site subset).
		var selectedDynamicSites = withHolySite
			.OrderByDescending(p => p.PrimaryImperatorProvince!.GetPopCount())
			.Take(4)
			.ToList();

		// Take the most populated territory without a holy site.
		if (bestWithout is not null) {
			selectedDynamicSites.Add(bestWithout);
		}

		return selectedDynamicSites
			.Select(p => landedTitles.GetBaronyForProvince(p.Id))
			.Where(t=>t is not null)!
			.ToList<Title>();
	}
}