using commonItems;
using System.Collections.Generic;
using System.Linq;

namespace ImperatorToCK3.Imperator.Countries {
	public partial class Country {
		private const string monarchyLawRegexStr = "succession_law|monarchy_military_reforms|monarchy_maritime_laws|monarchy_economic_law|monarchy_citizen_law" +
			"|monarchy_religious_laws|monarchy_legitimacy_laws|monarchy_contract_law|monarchy_divinity_statutes|jewish_monarchy_divinity_statutes|monarchy_subject_laws";
		private const string republicLawRegexStr = "republic_military_recruitment_laws_rom|republic_election_reforms_rom|corruption_laws_rom|republican_mediterranean_laws_rom|republican_religious_laws_rom|republic_integration_laws_rom|republic_citizen_laws_rom|republican_land_reforms_rom" +
			"|republic_military_recruitment_laws|republic_election_reforms|corruption_laws|republican_mediterranean_laws|republican_religious_laws|republic_integration_laws|republic_citizen_laws|republican_land_reforms";
		private const string tribalLawRegexStr = "tribal_religious_law|tribal_currency_laws|tribal_centralization_law|tribal_authority_laws|tribal_autonomy_laws|tribal_domestic_laws" +
			"|tribal_decentralized_laws|tribal_centralized_laws|tribal_super_decentralized_laws|tribal_super_centralized_laws";

		private static readonly SortedSet<string> monarchyGovernments = new() { "dictatorship", "despotic_monarchy", "aristocratic_monarchy", "stratocratic_monarchy", "theocratic_monarchy", "plutocratic_monarchy", "imperium", "imperial_cult" };
		private static readonly SortedSet<string> republicGovernments = new() { "aristocratic_republic", "theocratic_republic", "oligarchic_republic", "democratic_republic", "plutocratic_republic", "athenian_republic" };
		private static readonly SortedSet<string> tribalGovernments = new() { "tribal_chiefdom", "tribal_kingdom", "tribal_federation" };
		private static readonly Parser parser = new();
		private static Country country = new(0);
		public static HashSet<string> IgnoredTokens { get; private set; } = new();
		public string? PrimaryCulture { get; private set; }
		public string? Religion { get; private set; }

		static Country() {
			parser.RegisterKeyword("tag", reader =>
				country.Tag = reader.GetString()
			);
			parser.RegisterKeyword("country_name", reader =>
				country.CountryName = CountryName.Parse(reader)
			);
			parser.RegisterKeyword("flag", reader =>
				country.Flag = reader.GetString()
			);
			parser.RegisterKeyword("country_type", reader => {
				var countryTypeStr = reader.GetString();
				switch (countryTypeStr) {
					case "rebels":
						country.CountryType = CountryType.rebels;
						break;
					case "pirates":
						country.CountryType = CountryType.pirates;
						break;
					case "barbarians":
						country.CountryType = CountryType.barbarians;
						break;
					case "mercenaries":
						country.CountryType = CountryType.mercenaries;
						break;
					case "real":
						country.CountryType = CountryType.real;
						break;
					default:
						Logger.Error($"Unrecognized country type: {countryTypeStr}, defaulting to real.");
						country.CountryType = CountryType.real;
						break;
				}
			});
			parser.RegisterKeyword("color", reader =>
				country.Color1 = new ColorFactory().GetColor(reader)
			);
			parser.RegisterKeyword("color2", reader =>
				country.Color2 = new ColorFactory().GetColor(reader)
			);
			parser.RegisterKeyword("color3", reader =>
				country.Color3 = new ColorFactory().GetColor(reader)
			);
			parser.RegisterKeyword("currency_data", reader =>
				country.Currencies = new CountryCurrencies(reader)
			);
			parser.RegisterKeyword("capital", reader => {
				var capitalProvId = reader.GetULong();
				if (capitalProvId > 0) {
					country.Capital = capitalProvId;
				}
			});
			parser.RegisterKeyword("historical_regnal_numbers", reader => {
				country.HistoricalRegnalNumbers = new Dictionary<string, int>(
					reader.GetAssignments().ToDictionary(
						t => t.Key, t => int.Parse(t.Value)
					)
				);
			});
			parser.RegisterKeyword("primary_culture", reader =>
				country.PrimaryCulture = reader.GetString()
			);
			parser.RegisterKeyword("religion", reader =>
				country.Religion = reader.GetString()
			);
			parser.RegisterKeyword("government_key", reader => {
				var governmentStr = reader.GetString();
				country.Government = governmentStr;
				// set government type
				if (monarchyGovernments.Contains(governmentStr)) {
					country.GovernmentType = GovernmentType.monarchy;
				} else if (republicGovernments.Contains(governmentStr)) {
					country.GovernmentType = GovernmentType.republic;
				} else if (tribalGovernments.Contains(governmentStr)) {
					country.GovernmentType = GovernmentType.tribal;
				}
			});
			parser.RegisterKeyword("family", reader =>
				country.parsedFamilyIds.Add(reader.GetULong())
			);
			parser.RegisterKeyword("minor_family", reader =>
				country.parsedFamilyIds.Add(reader.GetULong())
			);
			parser.RegisterKeyword("monarch", reader =>
				country.monarchId = reader.GetULong()
			);
			parser.RegisterKeyword("ruler_term", reader =>
				country.RulerTerms.Add(RulerTerm.Parse(reader))
			);
			parser.RegisterRegex(monarchyLawRegexStr, reader =>
				country.monarchyLaws.Add(reader.GetString())
			);
			parser.RegisterRegex(republicLawRegexStr, reader =>
				country.republicLaws.Add(reader.GetString())
			);
			parser.RegisterRegex(tribalLawRegexStr, reader =>
				country.tribalLaws.Add(reader.GetString())
			);
			parser.RegisterRegex(CommonRegexes.Catchall, (reader, token) => {
				IgnoredTokens.Add(token);
				ParserHelpers.IgnoreItem(reader);
			});
		}
		public static Country Parse(BufferedReader reader, ulong countryId) {
			country = new Country(countryId);
			parser.ParseStream(reader);
			return country;
		}
	}
}
