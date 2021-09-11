using System;
using System.Collections.Generic;
using commonItems;
using ImperatorToCK3.Mappers.Localization;
using ImperatorToCK3.Mappers.Province;
using ImperatorToCK3.Mappers.CoA;
using ImperatorToCK3.Mappers.TagTitle;
using ImperatorToCK3.Mappers.Government;
using ImperatorToCK3.Mappers.SuccessionLaw;
using System.IO;

namespace ImperatorToCK3.CK3.Titles {
	public enum TitleRank { barony, county, duchy, kingdom, empire }
	public class Title : Parser {
		public Title() { }
		public Title(string name) {
			Name = name;
			SetRank();
		}
		public void InitializeFromTag(
			Imperator.Countries.Country country,
			Dictionary<ulong, Imperator.Countries.Country> imperatorCountries,
			LocalizationMapper localizationMapper,
			LandedTitles landedTitles,
			ProvinceMapper provinceMapper,
			CoaMapper coaMapper,
			TagTitleMapper tagTitleMapper,
			GovernmentMapper governmentMapper,
			SuccessionLawMapper successionLawMapper
		) {
			IsImportedOrUpdatedFromImperator = true;
			ImperatorCountry = country;

			// ------------------ determine CK3 title

			LocBlock? validatedName;
			// hard code for Antigonid Kingdom, Seleucid Empire and Maurya (which use customizable localization for name and adjective)
			if (ImperatorCountry.Name == "PRY_DYN") {
				validatedName = localizationMapper.GetLocBlockForKey("get_pry_name_fallback");
			} else if (ImperatorCountry.Name == "SEL_DYN") {
				validatedName = localizationMapper.GetLocBlockForKey("get_sel_name_fallback");
			} else if (ImperatorCountry.Name == "MRY_DYN") {
				validatedName = localizationMapper.GetLocBlockForKey("get_mry_name_fallback");
			}
			// normal case
			else {
				validatedName = ImperatorCountry.CountryName.GetNameLocBlock(localizationMapper, imperatorCountries);
			}

			string? title;
			if (validatedName is not null) {
				title = tagTitleMapper.GetTitleForTag(ImperatorCountry.Tag, ImperatorCountry.GetCountryRank(), validatedName.english);
			} else {
				title = tagTitleMapper.GetTitleForTag(ImperatorCountry.Tag, ImperatorCountry.GetCountryRank());
			}

			if (title is null) {
				throw new ArgumentException("Country " + ImperatorCountry.Tag + " could not be mapped!");
			}

			Name = title;

			SetRank();

			// ------------------ determine previous and current holders
			var firstPossibleDate = new Date(0, 1, 1); // there was no 0 AD, but year 0 works in game and serves well for adding BC characters to holder history

			foreach(var impRulerTerm in ImperatorCountry.RulerTerms) {
				var rulerTerm = new RulerTerm(impRulerTerm, governmentMapper);
				var characterId = rulerTerm.CharacterId;
				var gov = rulerTerm.Government;
				var startDate = rulerTerm.StartDate;
				if (startDate < firstPossibleDate) {
					startDate = firstPossibleDate; // TODO: remove this workaround when CK3 supports negative dates
					firstPossibleDate.ChangeByDays(1);
				}

				history.History.AddSimpleFieldValue("holder", characterId, startDate);
				if (gov is not null) {
					history.History.AddSimpleFieldValue("government", gov, startDate);
				}
			}

			// ------------------ determine government
			//if (ImperatorCountry.Government is not null) {
			//	history.Government = governmentMapper.GetCK3GovernmentForImperatorGovernment(ImperatorCountry.Government);
			//}

			// ------------------ determine color
			var color1Opt = ImperatorCountry.Color1;
			if (color1Opt is not null) {
				Color1 = color1Opt;
			}
			var color2Opt = ImperatorCountry.Color2;
			if (color2Opt is not null) {
				Color2 = color2Opt;
			}

			// determine successions laws
			SuccessionLaws = successionLawMapper.GetCK3LawsForImperatorLaws(ImperatorCountry.GetLaws());

			// ------------------ determine CoA
			CoA = coaMapper.GetCoaForFlagName(ImperatorCountry.Flag);

			// ------------------ determine other attributes

			var srcCapital = ImperatorCountry.Capital;
			if (srcCapital is not null) {
				var provMappingsForImperatorCapital = provinceMapper.GetCK3ProvinceNumbers((ulong)srcCapital);
				if (provMappingsForImperatorCapital.Count > 0) {
					var foundCounty = landedTitles.GetCountyForProvince(provMappingsForImperatorCapital[0]);
					if (foundCounty is not null) {
						CapitalCounty = new(foundCounty, null);
					}
				}
			}

			// ------------------ Country Name Locs

			var nameSet = false;
			if (validatedName is not null) {
				Localizations.Add(Name, validatedName);
				nameSet = true;
			}
			if (!nameSet) {
				var impTagLoc = localizationMapper.GetLocBlockForKey(ImperatorCountry.Tag);
				if (impTagLoc is not null) {
					Localizations.Add(Name, impTagLoc);
					nameSet = true;
				}
			}
			// giving up
			if (!nameSet) {
				Logger.Warn($"{Name} needs help with localization! {ImperatorCountry.Name}?");
			}

			// --------------- Adjective Locs
			TrySetAdjectiveLoc(localizationMapper, imperatorCountries);
		}
		public void UpdateFromTitle(Title otherTitle) {
			if (Name != otherTitle.Name) {
				Logger.Error($"{Name} can not be updated from {otherTitle.Name}: different title names!");
				return;
			}
			Name = otherTitle.Name;
			Localizations = otherTitle.Localizations;

			IsImportedOrUpdatedFromImperator = otherTitle.IsImportedOrUpdatedFromImperator;
			ImperatorCountry = otherTitle.ImperatorCountry;

			history = otherTitle.history;

			Color1 = otherTitle.Color1;
			Color2 = otherTitle.Color2;
			CoA = otherTitle.CoA;

			CapitalCounty = otherTitle.CapitalCounty;
		}
		public void LoadTitles(BufferedReader reader) {
			RegisterKeys();
			ParseStream(reader);
			ClearRegisteredRules();
		}

		public Date GetDateOfLastHolderChange() {
			var field = history.History.SimpleFields["holder"];
			var dates = new SortedSet<Date>(field.ValueHistory.Keys);
			var lastDate = dates.Max;
			return lastDate ?? new Date(1, 1, 1);
		}
		public string GetHolderId(Date date) {
			return history.GetHolderId(date);
		}
		public void SetHolderId(string id, Date date) {
			history.History.AddSimpleFieldValue("holder", id, date);
		}

		public List<RulerTerm> RulerTerms { get; private set; } = new();
		public int? DevelopmentLevel {
			get {
				return history.DevelopmentLevel;
			}
			set {
				history.DevelopmentLevel = value;
			}
		}

		public Dictionary<string, LocBlock> Localizations { get; set; } = new();
		public void SetNameLoc(LocBlock locBlock) {
			Localizations[Name] = locBlock;
		}
		private void TrySetAdjectiveLoc(LocalizationMapper localizationMapper, Dictionary<ulong, Imperator.Countries.Country> imperatorCountries) {
			if (ImperatorCountry is null) {
				Logger.Warn($"Cannot set adjective for CK3 Title {Name} from null Imperator Country!");
				return;
			}

			var adjSet = false;

			if (ImperatorCountry.Tag == "PRY" || ImperatorCountry.Tag == "SEL" || ImperatorCountry.Tag == "MRY") { // these tags use customizable loc for adj
				LocBlock? validatedAdj = null;
				if (ImperatorCountry.Name == "PRY_DYN") {
					validatedAdj = localizationMapper.GetLocBlockForKey("get_pry_adj_fallback");
				} else if (ImperatorCountry.Name == "SEL_DYN") {
					validatedAdj = localizationMapper.GetLocBlockForKey("get_sel_adj_fallback");
				} else if (ImperatorCountry.Name == "MRY_DYN") {
					validatedAdj = localizationMapper.GetLocBlockForKey("get_mry_adj_fallback");
				}

				if (validatedAdj is not null) {
					Localizations.Add(Name + "_adj", validatedAdj);
					adjSet = true;
				}
			}
			if (!adjSet) {
				var adjOpt = ImperatorCountry.CountryName.GetAdjectiveLocBlock(localizationMapper, imperatorCountries);
				if (adjOpt is not null) {
					Localizations.Add(Name + "_adj", adjOpt);
					adjSet = true;
				}
			}
			if (!adjSet) { // final fallback
				var adjLocalizationMatch = localizationMapper.GetLocBlockForKey(ImperatorCountry.Tag);
				if (adjLocalizationMatch is not null) {
					Localizations.Add(Name + "_adj", adjLocalizationMatch);
					adjSet = true;
				}
			}
			// giving up
			if (!adjSet) {
				Logger.Warn($"{Name} needs help with localization for adjective! {ImperatorCountry.Name}_adj?");
			}
		}
		public void AddHistory(LandedTitles landedTitles, TitleHistory titleHistory) {
			history = titleHistory;
			if (history.Liege is not null) {
				if (landedTitles.StoredTitles.TryGetValue(history.Liege, out var liege)) {
					DeFactoLiege = liege;
				}
			}
		}

		public string? CoA { get; private set; }
		public KeyValuePair<string, Title?>? CapitalCounty { get; private set; }
		public Imperator.Countries.Country? ImperatorCountry { get; private set; }
		public Color? Color1 { get; private set; }
		public Color? Color2 { get; private set; }
		public Color? Color { get; private set; } // TODO: CHECK DIFFERENCE BETWEEN COLOR AND COLOR1 AND COLOR2

		private Title? deJureLiege;
		public Title? DeJureLiege { // direct de jure liege title name, e.g. e_hispania
			get {
				return deJureLiege;
			}
			set {
				deJureLiege = value;
				if (value is not null) {
					value.DeJureVassals[Name] = this;
				}
			}
		}
		private Title? deFactoLiege;
		public Title? DeFactoLiege { // direct de facto liege title name, e.g. e_hispania
			get {
				return deFactoLiege;
			}
			set {
				deFactoLiege = value;
				if (value is not null) {
					value.DeFactoVassals[Name] = this;
				}
			}
		}
		public Dictionary<string, Title> DeJureVassals { get; private set; } = new(); // DIRECT de jure vassals
		public Dictionary<string, Title> GetDeJureVassalsAndBelow() {
			return GetDeJureVassalsAndBelow("bcdke");
		}
		public Dictionary<string, Title> GetDeJureVassalsAndBelow(string rankFilter) {
			var rankFilterAsArray = rankFilter.ToCharArray();
			Dictionary<string, Title> deJureVassalsAndBelow = new();
			foreach (var (vassalTitleName, vassalTitle) in DeJureVassals) {
				// add the direct part
				if (vassalTitleName.IndexOfAny(rankFilterAsArray) == 0) {
					deJureVassalsAndBelow[vassalTitleName] = vassalTitle;
				}

				// add the "below" part (recursive)
				var belowTitles = vassalTitle.GetDeJureVassalsAndBelow(rankFilter);
				foreach (var (belowTitleName, belowTitle) in belowTitles) {
					if (belowTitleName.IndexOfAny(rankFilterAsArray) == 0) {
						deJureVassalsAndBelow[belowTitleName] = belowTitle;
					}
				}
			}
			return deJureVassalsAndBelow;
		}
		public Dictionary<string, Title> DeFactoVassals { get; private set; } = new(); // DIRECT de facto vassals
		public Dictionary<string, Title> GetDeFactoVassalsAndBelow() {
			return GetDeFactoVassalsAndBelow("bcdke");
		}
		public Dictionary<string, Title> GetDeFactoVassalsAndBelow(string rankFilter) {
			var rankFilterAsArray = rankFilter.ToCharArray();
			Dictionary<string, Title> deFactoVassalsAndBelow = new();
			foreach (var (vassalTitleName, vassalTitle) in DeFactoVassals) {
				// add the direct part
				if (vassalTitleName.IndexOfAny(rankFilterAsArray) == 0) {
					deFactoVassalsAndBelow[vassalTitleName] = vassalTitle;
				}

				// add the "below" part (recursive)
				var belowTitles = vassalTitle.GetDeFactoVassalsAndBelow(rankFilter);
				foreach (var (belowTitleName, belowTitle) in belowTitles) {
					if (belowTitleName.IndexOfAny(rankFilterAsArray) == 0) {
						deFactoVassalsAndBelow[belowTitleName] = belowTitle;
					}
				}
			}
			return deFactoVassalsAndBelow;
		}

		public string Name { get; private set; } = string.Empty; // e.g. d_latium
		public TitleRank Rank { get; private set; } = TitleRank.duchy;
		public bool Landless { get; private set; } = false;
		public bool HasDefiniteForm { get; private set; } = false;
		public string? Government => history.Government;
		public int? OwnOrInheritedDevelopmentLevel {
			get {
				if (history.DevelopmentLevel is not null) { // if development level is already set, just return it
					return history.DevelopmentLevel;
				}
				if (deJureLiege is not null) { // if de jure liege exists, return their level
					return deJureLiege.OwnOrInheritedDevelopmentLevel;
				}
				return null;
			}
		}
		public SortedSet<string> SuccessionLaws { get; private set; } = new();
		public bool IsImportedOrUpdatedFromImperator { get; private set; } = false;

		private void RegisterKeys() {
			RegisterRegex(@"(k|d|c|b)_[A-Za-z0-9_\-\']+", (reader, titleNameStr) => {
				// Pull the titles beneath this one and add them to the lot, overwriting existing ones.
				var newTitle = new Title(titleNameStr);
				newTitle.LoadTitles(reader);

				if (newTitle.Rank == TitleRank.barony && string.IsNullOrEmpty(CapitalBarony)) { // title is a barony, and no other barony has been found in this scope yet
					CapitalBarony = newTitle.Name;
				}

				AddFoundTitle(newTitle, foundTitles);
				newTitle.DeJureLiege = this;
			});
			RegisterKeyword("definite_form", reader => {
				HasDefiniteForm = ParserHelpers.GetString(reader) == "yes";
			});
			RegisterKeyword("landless", reader => {
				Landless = ParserHelpers.GetString(reader) == "yes";
			});
			RegisterKeyword("color", reader => {
				Color = colorFactory.GetColor(reader);
			});
			RegisterKeyword("capital", reader => {
				CapitalCounty = new(ParserHelpers.GetString(reader), null);
			});
			RegisterKeyword("province", reader => {
				Province = ParserHelpers.GetULong(reader);
			});
			RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreItem);
		}

		internal static void AddFoundTitle(Title newTitle, Dictionary<string, Title> foundTitles) {
			foreach (var (locatedTitleName, locatedTitle) in newTitle.foundTitles) {
				if (newTitle.Rank == TitleRank.county) {
					var baronyProvince = locatedTitle.Province;
					if (baronyProvince is not null) {
						if (locatedTitleName == newTitle.CapitalBarony) {
							newTitle.CapitalBaronyProvince = (ulong)baronyProvince;
						}
						newTitle.AddCountyProvince((ulong)baronyProvince); // add found baronies' provinces to countyProvinces
					}
				}
				foundTitles[locatedTitleName] = locatedTitle;
			}
			// now that all titles under newTitle have been moved to main foundTitles, newTitle's foundTitles can be cleared
			newTitle.foundTitles.Clear();

			// And then add this one as well, overwriting existing.
			foundTitles[newTitle.Name] = newTitle;
		}

		private TitleHistory history = new();
		private readonly Dictionary<string, Title> foundTitles = new(); // title name, title. Titles are only held here during loading of landed_titles, then they are cleared		// used by duchy titles only

		private static readonly ColorFactory colorFactory = new();

		private void SetRank() {
			if (Name.StartsWith('b')) {
				Rank = TitleRank.barony;
			} else if (Name.StartsWith('c')) {
				Rank = TitleRank.county;
			} else if (Name.StartsWith('d')) {
				Rank = TitleRank.duchy;
			} else if (Name.StartsWith('k')) {
				Rank = TitleRank.kingdom;
			} else if (Name.StartsWith('e')) {
				Rank = TitleRank.empire;
			} else {
				throw new FormatException("Title " + Name + ": unknown rank!");
			}
		}

		public void OutputHistory(StreamWriter writer, Date ck3BookmarkDate) {
			writer.WriteLine(Name + " = {");

			if (history.History.SimpleFields.ContainsKey("holder")) {
				foreach (var (date, holderId) in history.History.SimpleFields["holder"].ValueHistory) {
					writer.WriteLine($"\t{date} = {{ holder = {holderId} }}");
				}
			}
			if (history.History.SimpleFields.ContainsKey("government")) {
				foreach (var (date, government) in history.History.SimpleFields["government"].ValueHistory) {
					writer.WriteLine($"\t{date} = {{ government = {government} }}");
				}
			}

			writer.WriteLine($"\t{ck3BookmarkDate} = {{");

			var deFactoLiege = DeFactoLiege;
			if (deFactoLiege is not null) {
				writer.WriteLine("\t\tliege = " + deFactoLiege.Name);
			}

			if (Government is not null) {
				writer.WriteLine("\t\tgovernment = " + Government);
			}

			var succLaws = SuccessionLaws;
			if (succLaws.Count > 0) {
				writer.WriteLine("\t\tsuccession_laws = {");
				foreach (var law in succLaws) {
					writer.WriteLine("\t\t\t" + law);
				}
				writer.WriteLine("\t\t}");
			}

			if (Rank != TitleRank.barony) {
				var developmentLevelOpt = DevelopmentLevel;
				if (developmentLevelOpt is not null) {
					writer.WriteLine("\t\tchange_development_level = " + developmentLevelOpt);
				}
			}

			writer.WriteLine("\t}");

			writer.WriteLine("}");
		}

		// used by kingdom titles only
		public bool KingdomContainsProvince(ulong provinceID) {
			if (Rank != TitleRank.kingdom) {
				return false;
			}

			foreach (var vassal in DeJureVassals.Values) {
				if (vassal?.Rank == TitleRank.duchy && vassal.DuchyContainsProvince(provinceID)) {
					return true;
				}
			}
			return false;
		}

		// used by duchy titles only
		public bool DuchyContainsProvince(ulong provinceID) {
			if (Rank != TitleRank.duchy) {
				return false;
			}

			foreach (var vassal in DeJureVassals.Values) {
				if (vassal?.Rank == TitleRank.county && vassal.CountyProvinces.Contains(provinceID)) {
					return true;
				}
			}
			return false;
		}

		// used by county titles only
		public void AddCountyProvince(ulong provinceID) {
			CountyProvinces.Add(provinceID);
		}
		public SortedSet<ulong> CountyProvinces { get; private set; } = new();
		public string CapitalBarony { get; private set; } = string.Empty; // used when parsing inside county to save first barony
		public ulong CapitalBaronyProvince { get; private set; } = 0; // county barony's province; 0 is not a valid barony ID

		// used by barony titles only
		public ulong? Province { get; private set; } // province is area on map. b_ barony is its corresponding title.
	}
}
