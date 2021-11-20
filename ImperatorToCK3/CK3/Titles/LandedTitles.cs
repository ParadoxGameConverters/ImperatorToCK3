using commonItems;
using System.Collections.Generic;
using System.Linq;

namespace ImperatorToCK3.CK3.Titles {
	// This is a recursive class that scrapes 00_landed_titles.txt (and related files) looking for title colors, landlessness,
	// and most importantly relation between baronies and barony provinces so we can link titles to actual clay.
	// Since titles are nested according to hierarchy we do this recursively.
	public class LandedTitles : Dictionary<string, Title> { // <title name, title> dictionary
		public void LoadTitles(string fileName) {
			var parser = new Parser();
			RegisterKeys(parser);
			parser.ParseFile(fileName);
			Logger.Debug($"Ignored Title tokens: {string.Join(", ", Title.IgnoredTokens)}");

			LinkCapitals();
		}
		public void LoadTitles(BufferedReader reader) {
			var parser = new Parser();
			RegisterKeys(parser);
			parser.ParseStream(reader);
			Logger.Debug($"Ignored Title tokens: {string.Join(", ", Title.IgnoredTokens)}");

			LinkCapitals();
		}
		public void InsertTitle(Title? title) {
			if (title is null) {
				Logger.Warn("Cannot insert null Title to LandedTitles!");
				return;
			}
			if (!string.IsNullOrEmpty(title.Name)) {
				this[title.Name] = title;
				title.LinkCapital(this);
			} else {
				Logger.Warn("Not inserting a Title with empty name!");
			}
		}
		public void EraseTitle(string name) {
			if (TryGetValue(name, out var titleToErase)) {
				var deJureLiege = titleToErase.DeJureLiege;
				if (deJureLiege is not null) {
					deJureLiege.DeJureVassals.Remove(name);
				}

				var deFactoLiege = titleToErase.DeFactoLiege;
				if (deFactoLiege is not null) {
					deFactoLiege.DeFactoVassals.Remove(name);
				}

				foreach (var vassal in titleToErase.DeJureVassals.Values) {
					vassal.DeJureLiege = null;
				}
				foreach (var vassal in titleToErase.DeFactoVassals.Values) {
					vassal.DeFactoLiege = null;
				}

				if (titleToErase.ImperatorCountry is not null) {
					titleToErase.ImperatorCountry.CK3Title = null;
				}
			}
			Remove(name);
		}
		public Title? GetCountyForProvince(ulong provinceId) {
			foreach (var county in Values.Where(title => title.Rank == TitleRank.county)) {
				if (county.CountyProvinces.Contains(provinceId)) {
					return county;
				}
			}
			return null;
		}

		private void RegisterKeys(Parser parser) {
			parser.RegisterRegex(@"(e|k|d|c|b)_[A-Za-z0-9_\-\']+", (reader, titleNameStr) => {
				// Pull the titles beneath this one and add them to the lot, overwriting existing ones.
				var newTitle = new Title(titleNameStr);
				newTitle.LoadTitles(reader);

				Title.AddFoundTitle(newTitle, this);
			});
			parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		}

		private void LinkCapitals() {
			foreach (var title in Values) {
				title.LinkCapital(this);
			}
		}
	}
}
