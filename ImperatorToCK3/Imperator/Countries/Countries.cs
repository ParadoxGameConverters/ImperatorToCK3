using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using commonItems;
using ImperatorToCK3.Imperator.Families;

namespace ImperatorToCK3.Imperator.Countries {
	public class Countries : Parser {
		public Dictionary<ulong, Country?> StoredCountries { get; } = new();

		public Countries() { }
		public Countries(BufferedReader reader) {
			RegisterKeys();
			ParseStream(reader);
			ClearRegisteredRules();
		}
		private void RegisterKeys() {
			RegisterRegex(CommonRegexes.Integer, (reader, countryID) => {
				var newCountry = Country.Parse(reader, ulong.Parse(countryID));
				StoredCountries.Add(newCountry.ID, newCountry);
			});
			RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		}
		public void LinkFamilies(Families.Families families) {
			var counter = 0;
			SortedSet<ulong> idsWithoutDefinition = new();
			foreach (var country in StoredCountries.Values) {
				if (country is null) {
					Logger.Warn("Skipping linking null country to family");
					continue;
				}
				if (country.Families.Count > 0) {
					var newFamilies = new Dictionary<ulong, Families.Family?>();
					foreach (var familyID in country.Families.Keys) {
						if (families.StoredFamilies.TryGetValue(familyID, out var familyToLink)) {
							newFamilies.Add(familyID, familyToLink);
							++counter;
						} else {
							idsWithoutDefinition.Add(familyID);
						}
					}
					country.SetFamilies(newFamilies);
				}
			}

			if (idsWithoutDefinition.Count > 0) {
				var warningBuilder = new StringBuilder();
				warningBuilder.Append("Families without definition:");
				foreach(var id in idsWithoutDefinition) {
					warningBuilder.Append(' ');
					warningBuilder.Append(id);
					warningBuilder.Append(',');
				}
				var warningStr = warningBuilder.ToString();
				Logger.Debug(warningStr[0..^1]); // remove last comma
			}

			Logger.Info($"{counter} families linked to countries.");
		}
		public static Countries ParseBloc(BufferedReader reader) {
			var blocParser = new Parser();
			Countries countries = new();
			blocParser.RegisterKeyword("country_database", reader =>
				countries = new Countries(reader)
			);
			blocParser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);

			blocParser.ParseStream(reader);
			blocParser.ClearRegisteredRules();

			return countries;
		}
	}
}
