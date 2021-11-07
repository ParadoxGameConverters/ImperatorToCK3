using commonItems;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace ImperatorToCK3.CK3.Titles {
	// This is a recursive class that scrapes 00_landed_titles.txt (and related files) looking for title colors, landlessness,
	// and most importantly relation between baronies and barony provinces so we can link titles to actual clay.
	// Since titles are nested according to hierarchy we do this recursively.
	public class LandedTitles : Parser {
		public void LoadTitles(string fileName) {
			RegisterKeys();
			ParseFile(fileName);
			ClearRegisteredRules();
			Logger.Debug("Ignored Title tokens: " + string.Join(", ", Title.IgnoredTokens));

			LinkCapitals();
		}
		public void LoadTitles(BufferedReader reader) {
			RegisterKeys();
			ParseStream(reader);
			ClearRegisteredRules();
			Logger.Debug("Ignored Title tokens: " + string.Join(", ", Title.IgnoredTokens));

			LinkCapitals();
		}
		public void InsertTitle(Title? title) {
			if (title is null) {
				Logger.Warn("Cannot insert null Title to LandedTitles!");
				return;
			}
			if (!string.IsNullOrEmpty(title.Name)) {
				titlesDict[title.Name] = title;
				title.LinkCapital(titlesDict);
			} else {
				Logger.Warn("Not inserting a Title with empty name!");
			}
		}
		public void EraseTitle(string name) {
			if (TryGetTitle(name, out var titleToErase)) {
				var deJureLiege = titleToErase.DeJureLiege;
				if (deJureLiege is not null) {
					deJureLiege.DeJureVassals.Remove(name);
				}

				foreach (var vassal in titleToErase.DeJureVassals.Values) {
					vassal.DeJureLiege = null;
				}

				foreach (var title in StoredTitles) {
					title.RemoveDeFactoLiegeReferences(name);
				}

				if (titleToErase.ImperatorCountry is not null) {
					titleToErase.ImperatorCountry.CK3Title = null;
				}
			}
			titlesDict.Remove(name);
		}
		public Title? GetCountyForProvince(ulong provinceId) {
			var counties = titlesDict.Values.Where(title => title.Rank == TitleRank.county);
			foreach (var county in counties) {
				if (county.CountyProvinces.Contains(provinceId)) {
					return county;
				}
			}
			return null;
		}

		public bool TryGetTitle(string titleName, [NotNullWhen(returnValue: true)] out Title? title) {
			return titlesDict.TryGetValue(titleName, out title);
		}
		public Title this[string titleName] => titlesDict[titleName];
		public Dictionary<string, Title>.ValueCollection StoredTitles => titlesDict.Values;

		private readonly Dictionary<string, Title> titlesDict = new(); // titleName, title

		private void RegisterKeys() {
			RegisterRegex(@"(e|k|d|c|b)_[A-Za-z0-9_\-\']+", (reader, titleNameStr) => {
				// Pull the titles beneath this one and add them to the lot, overwriting existing ones.
				var newTitle = new Title(titleNameStr);
				newTitle.LoadTitles(reader);

				Title.AddFoundTitle(newTitle, titlesDict);
			});
			RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		}

		private void LinkCapitals() {
			foreach (var title in titlesDict.Values) {
				title.LinkCapital(titlesDict);
			}
		}
	}
}
