using commonItems;
using commonItems.Colors;
using commonItems.Mods;
using ImperatorToCK3.CommonUtils;
using System.Collections.Generic;
using System.Linq;

namespace ImperatorToCK3.Imperator.Countries;

public sealed partial class Country {
	private const string monarchyLawRegexStr = "succession_law|monarchy_military_reforms|monarchy_maritime_laws|monarchy_economic_law|monarchy_citizen_law" +
	                                           "|monarchy_religious_laws|monarchy_legitimacy_laws|monarchy_contract_law|monarchy_divinity_statutes|jewish_monarchy_divinity_statutes|monarchy_subject_laws";
	private const string republicLawRegexStr = "republic_military_recruitment_laws_rom|republic_election_reforms_rom|corruption_laws_rom|republican_mediterranean_laws_rom|republican_religious_laws_rom|republic_integration_laws_rom|republic_citizen_laws_rom|republican_land_reforms_rom" +
	                                           "|republic_military_recruitment_laws|republic_election_reforms|corruption_laws|republican_mediterranean_laws|republican_religious_laws|republic_integration_laws|republic_citizen_laws|republican_land_reforms";
	private const string tribalLawRegexStr = "tribal_religious_law|tribal_currency_laws|tribal_centralization_law|tribal_authority_laws|tribal_autonomy_laws|tribal_domestic_laws" +
	                                         "|tribal_decentralized_laws|tribal_centralized_laws|tribal_super_decentralized_laws|tribal_super_centralized_laws";

	private static readonly SortedSet<string> monarchyGovernments = new();
	private static readonly SortedSet<string> republicGovernments = new();
	private static readonly SortedSet<string> tribalGovernments = new();
	public static ConcurrentIgnoredKeywordsSet IgnoredTokens { get; } = new();

	private static void RegisterCountryKeywords(Parser parser, Country parsedCountry) {
		var colorFactory = new ColorFactory();
		
		parser.RegisterKeyword("tag", reader => parsedCountry.Tag = reader.GetString());
		parser.RegisterKeyword("historical", reader => parsedCountry.HistoricalTag = reader.GetString());
		parser.RegisterKeyword("origin", reader => parsedCountry.parsedOriginCountryId = reader.GetULong());
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
		parser.RegisterKeyword("color", reader => parsedCountry.Color1 = colorFactory.GetColor(reader));
		parser.RegisterKeyword("color2", reader => parsedCountry.Color2 = colorFactory.GetColor(reader));
		parser.RegisterKeyword("color3", reader => parsedCountry.Color3 = colorFactory.GetColor(reader));
		parser.RegisterKeyword("currency_data", reader =>
			parsedCountry.Currencies = new CountryCurrencies(reader)
		);
		parser.RegisterKeyword("capital", reader => {
			var capitalProvId = reader.GetULong();
			if (capitalProvId > 0) {
				parsedCountry.CapitalProvinceId = capitalProvId;
			}
		});
		parser.RegisterKeyword("historical_regnal_numbers", reader => {
			parsedCountry.HistoricalRegnalNumbers = reader.GetAssignments()
				.GroupBy(a => a.Key)
				.ToDictionary(g => g.Key, g => int.Parse(g.Last().Value));
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
		parser.RegisterKeyword("active_inventions", reader => {
			parsedCountry.inventionBooleans.AddRange(reader.GetInts().Select(i => i != 0));
		});
		parser.RegisterKeyword("mark_invention", ParserHelpers.IgnoreItem);
		parser.RegisterKeyword("ruler_term", reader => parsedCountry.RulerTerms.Add(RulerTerm.Parse(reader)));
		parser.RegisterRegex(monarchyLawRegexStr, reader => parsedCountry.monarchyLaws.Add(reader.GetString()));
		parser.RegisterRegex(republicLawRegexStr, reader => parsedCountry.republicLaws.Add(reader.GetString()));
		parser.RegisterRegex(tribalLawRegexStr, reader => parsedCountry.tribalLaws.Add(reader.GetString()));
		parser.RegisterKeyword("is_antagonist", ParserHelpers.IgnoreItem);
		parser.RegisterKeyword("has_senior_ally", ParserHelpers.IgnoreItem);
		parser.RegisterKeyword("cached_happiness_for_owned", ParserHelpers.IgnoreItem);
		parser.RegisterKeyword("cached_pop_count_for_owned", ParserHelpers.IgnoreItem);
		parser.RegisterRegex(CommonRegexes.Catchall, (reader, token) => {
			IgnoredTokens.Add(token);
			ParserHelpers.IgnoreItem(reader);
		});
	}
	public static Country Parse(BufferedReader reader, ulong countryId) {
		var newCountry = new Country(countryId);
		
		var parser = new Parser();
		RegisterCountryKeywords(parser, newCountry);
		parser.ParseStream(reader);
		
		return newCountry;
	}

	public static void LoadGovernments(ModFilesystem imperatorModFS) {
		Logger.Info("Loading Imperator governments...");
		string governmentType = "monarchy";

		var governmentParser = new Parser();
		governmentParser.RegisterKeyword("type", reader => governmentType = reader.GetString());
		governmentParser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreItem);

		var fileParser = new Parser();
		fileParser.RegisterRegex(CommonRegexes.String, (reader, govName) => {
			governmentType = "monarchy"; // default, overridden by parsed type
			governmentParser.ParseStream(reader);

			switch (governmentType) {
				case "republic":
					AddRepublicGovernment(govName);
					break;
				case "monarchy":
					AddMonarchyGovernment(govName);
					break;
				case "tribal":
					AddTribalGovernment(govName);
					break;
				default:
					AddMonarchyGovernment(govName);
					break;
			}
		});
		fileParser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		fileParser.ParseGameFolder("common/governments", imperatorModFS, "txt", recursive: true);
		Logger.IncrementProgress();

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