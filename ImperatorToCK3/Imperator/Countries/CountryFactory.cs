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

		private static readonly SortedSet<string> monarchyGovernments = new();
		private static readonly SortedSet<string> republicGovernments = new();
		private static readonly SortedSet<string> tribalGovernments = new();
		private static readonly Parser parser = new();
		private static Country parsedCountry = new(0);
		public static HashSet<string> IgnoredTokens { get; private set; } = new();

		static Country() {
			parser.RegisterKeyword("tag", reader => parsedCountry.Tag = reader.GetString());
			parser.RegisterKeyword("country_name", reader => parsedCountry.CountryName = CountryName.Parse(reader));
			parser.RegisterKeyword("flag", reader => parsedCountry.Flag = reader.GetString());
			parser.RegisterKeyword("country_type", reader => {
				var countryTypeStr = reader.GetString();
				switch (countryTypeStr) {
					case "rebels":
						parsedCountry.CountryType = CountryType.rebels;
						break;
					case "pirates":
						parsedCountry.CountryType = CountryType.pirates;
						break;
					case "barbarians":
						parsedCountry.CountryType = CountryType.barbarians;
						break;
					case "mercenaries":
						parsedCountry.CountryType = CountryType.mercenaries;
						break;
					case "real":
						parsedCountry.CountryType = CountryType.real;
						break;
					default:
						Logger.Error($"Unrecognized country type: {countryTypeStr}, defaulting to real.");
						parsedCountry.CountryType = CountryType.real;
						break;
				}
			});
			parser.RegisterKeyword("color", reader => parsedCountry.Color1 = new ColorFactory().GetColor(reader));
			parser.RegisterKeyword("color2", reader => parsedCountry.Color2 = new ColorFactory().GetColor(reader));
			parser.RegisterKeyword("color3", reader => parsedCountry.Color3 = new ColorFactory().GetColor(reader));
			parser.RegisterKeyword("currency_data", reader =>
				parsedCountry.Currencies = new CountryCurrencies(reader)
			);
			parser.RegisterKeyword("capital", reader => {
				var capitalProvId = reader.GetULong();
				if (capitalProvId > 0) {
					parsedCountry.Capital = capitalProvId;
				}
			});
			parser.RegisterKeyword("historical_regnal_numbers", reader => {
				parsedCountry.HistoricalRegnalNumbers = new Dictionary<string, int>(
					reader.GetAssignments().ToDictionary(
						t => t.Key, t => int.Parse(t.Value)
					)
				);
			});
			parser.RegisterKeyword("primary_culture", reader => parsedCountry.PrimaryCulture = reader.GetString());
			parser.RegisterKeyword("religion", reader => parsedCountry.Religion = reader.GetString());
			parser.RegisterKeyword("government_key", reader => {
				var governmentStr = reader.GetString();
				parsedCountry.Government = governmentStr;
				// set government type
				if (monarchyGovernments.Contains(governmentStr)) {
					parsedCountry.GovernmentType = GovernmentType.monarchy;
				} else if (republicGovernments.Contains(governmentStr)) {
					parsedCountry.GovernmentType = GovernmentType.republic;
				} else if (tribalGovernments.Contains(governmentStr)) {
					parsedCountry.GovernmentType = GovernmentType.tribal;
				}
			});
			parser.RegisterKeyword("family", reader => parsedCountry.parsedFamilyIds.Add(reader.GetULong()));
			parser.RegisterKeyword("minor_family", reader => parsedCountry.parsedFamilyIds.Add(reader.GetULong()));
			parser.RegisterKeyword("monarch", reader => parsedCountry.monarchId = reader.GetULong());
			parser.RegisterKeyword("ruler_term", reader => parsedCountry.RulerTerms.Add(RulerTerm.Parse(reader)));
			parser.RegisterRegex(monarchyLawRegexStr, reader => parsedCountry.monarchyLaws.Add(reader.GetString()));
			parser.RegisterRegex(republicLawRegexStr, reader => parsedCountry.republicLaws.Add(reader.GetString()));
			parser.RegisterRegex(tribalLawRegexStr, reader => parsedCountry.tribalLaws.Add(reader.GetString()));
			parser.RegisterRegex(CommonRegexes.Catchall, (reader, token) => {
				IgnoredTokens.Add(token);
				ParserHelpers.IgnoreItem(reader);
			});
		}
		public static Country Parse(BufferedReader reader, ulong countryId) {
			parsedCountry = new Country(countryId);
			parser.ParseStream(reader);
			return parsedCountry;
		}

		public static void LoadGovernments(Configuration config) {
			string governmentName = string.Empty;
			bool typeSpecified = false;

			var governmentParser = new Parser();
			governmentParser.RegisterKeyword("type", reader => {
				switch (reader.GetString()) {
					case "republic":
						AddRepublicGovernment(governmentName);
						typeSpecified = true;
						break;
					case "monarchy":
						AddMonarchyGovernment(governmentName);
						typeSpecified = true;
						break;
					case "tribal":
						AddTribalGovernment(governmentName);
						typeSpecified = true;
						break;
				}
			});

			var fileParser = new Parser();
			fileParser.RegisterRegex(CommonRegexes.String, (reader, govName) => {
				typeSpecified = false;
				governmentName = govName;
				governmentParser.ParseStream(reader);
				if (!typeSpecified) {
					// monarchy is the default type
					AddMonarchyGovernment(governmentName);
				}
			});

			static void AddRepublicGovernment(string name) {
				republicGovernments.Add(name);
				monarchyGovernments.Remove(name);
				tribalGovernments.Remove(name);
			}
			static void AddMonarchyGovernment(string name) {
				republicGovernments.Remove(name);
				tribalGovernments.Remove(name);
				monarchyGovernments.Add(name);
			}
			static void AddTribalGovernment(string name) {
				republicGovernments.Remove(name);
				monarchyGovernments.Remove(name);
				tribalGovernments.Add(name);
			}
		}
	}
}
