using System.Collections.Generic;
using commonItems;

namespace ImperatorToCK3.CK3.Titles {
	// This is a recursive class that scrapes 00_landed_titles.txt (and related files) looking for title colors, landlessness,
	// and most importantly relation between baronies and barony provinces so we can link titles to actual clay.
	// Since titles are nested according to hierarchy we do this recursively.
	public class LandedTitles : Parser {
		public void LoadTitles(string fileName) {
			RegisterKeys();
			ParseFile(fileName);
			ClearRegisteredRules();
		}
		public void LoadTitles(BufferedReader reader) {
			RegisterKeys();
			ParseStream(reader);
			ClearRegisteredRules();
		}
		public void InsertTitle(Title? title) {
			if (title is null) {
				Logger.Warn("Cannot insert null Title to LandedTitles!");
				return;
			}
			if (!string.IsNullOrEmpty(title.Name)) {
				StoredTitles[title.Name] = title;
			} else {
				Logger.Warn("Not inserting a Title with empty name!");
			}
		}
		public void EraseTitle(string name) {
			if (StoredTitles.TryGetValue(name, out var titleToErase)) {
				if (titleToErase is not null) {
					var deJureLiege = titleToErase.DeJureLiege;
					if (deJureLiege is not null) {
						deJureLiege.DeJureVassals.Remove(name);
					}

					var deFactoLiege = titleToErase.DeFactoLiege;
					if (deFactoLiege is not null) {
						deFactoLiege.DeFactoVassals.Remove(name);
					}

					foreach (var vassal in titleToErase.DeJureVassals.Values) {
						if (vassal is not null) {
							vassal.DeJureLiege = null;
						}
					}
					foreach (var vassal in titleToErase.DeFactoVassals.Values) {
						if (vassal is not null) {
							vassal.DeFactoLiege = null;
						}
					}

					titleToErase.ImperatorCountry?.SetCK3Title(null);
				}
			}
			StoredTitles.Remove(name);
		}
		public string? GetCountyForProvince(ulong provinceID) {
			foreach (var (titleName, title) in StoredTitles) {
				if (title?.Rank == TitleRank.county && title.CountyProvinces.Contains(provinceID)) {
					return titleName;
				}
			}
			return null;
		}

		public Dictionary<string, Title> StoredTitles { get; } = new(); // title name, title

		private void RegisterKeys() {
			RegisterRegex(@"(e|k|d|c|b)_[A-Za-z0-9_\-\']+", (reader, titleNameStr) => {
				// Pull the titles beneath this one and add them to the lot, overwriting existing ones.
				var newTitle = new Title(titleNameStr);
				newTitle.LoadTitles(reader);

				Title.AddFoundTitle(newTitle, StoredTitles);
			});
			RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		}
	}
}
