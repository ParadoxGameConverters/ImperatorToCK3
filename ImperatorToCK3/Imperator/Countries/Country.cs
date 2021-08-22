using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using commonItems;

namespace ImperatorToCK3.Imperator.Countries {
	public enum CountryType { rebels, pirates, barbarians, mercenaries, real }
	public enum CountryRank { migrantHorde, cityState, localPower, regionalPower, majorPower, greatPower }
	public enum GovernmentType { monarchy, republic, tribal }
	public class Country {
		public ulong ID { get; } = 0;
		public ulong? Monarch { get; private set; }  // >=0 are valid
		public string Tag { get; private set; } = "";
		public string Name => CountryName.Name;
		public CountryName CountryName { get; private set; } = new();
		public string Flag { get; private set; } = "";
		public CountryType CountryType { get; private set; } = CountryType.real;
		public ulong? Capital { get; private set; }
		public string? Government { get; private set; }
		public GovernmentType GovernmentType { get; private set; } = GovernmentType.monarchy;
		private readonly SortedSet<string> monarchyLaws = new();
		private readonly SortedSet<string> republicLaws = new();
		private readonly SortedSet<string> tribalLaws = new();
		public Color? Color1 { get; private set; }
		public Color? Color2 { get; private set; }
		public Color? Color3 { get; private set; }
		public CountryCurrencies Currencies { get; private set; } = new();
		public Dictionary<ulong, Family?> Families { get; private set; } = new();
		private readonly SortedSet<Provinces.Province> ownedProvinces = new();

		//ImperatorToCK3.CK3.Titles.Title? ck3Title = new(); // TODO: ENABLE

		public Country(ulong ID) {
			this.ID = ID;
		}
		public SortedSet<string> GetLaws() {
			return GovernmentType switch {
				GovernmentType.monarchy => monarchyLaws,
				GovernmentType.republic => republicLaws,
				GovernmentType.tribal => tribalLaws,
				_ => monarchyLaws,
			};
		}
		public CountryRank GetCountryRank() {
			var provCount = ownedProvinces.Count;
			if (provCount == 0) {
				return CountryRank.migrantHorde;
			}
			if (provCount == 1) {
				return CountryRank.cityState;
			}
			if (provCount <= 24) {
				return CountryRank.localPower;
			}
			if (provCount <= 99) {
				return CountryRank.regionalPower;
			}
			if (provCount <= 499) {
				return CountryRank.majorPower;
			}
			return CountryRank.greatPower;
		}
		public void SetFamilies(Dictionary<ulong, Family?> newFamilies) {
			Families = newFamilies;
		}
		public void RegisterProvince(Provinces.Province? province) {
			if (province is null) {
				Logger.Warn($"Didn't register null province to country {Name}.");
			} else {
				ownedProvinces.Add(province);
			}
		}
		/*
		public void SetCK3Title(ImperatorToCK3.CK3.Titles.Title? theTitle) { // TODO: ENABLE
			ck3Title = theTitle;
		}
		*/
		private static class CountryFactory {
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
			static CountryFactory() {
				parser.RegisterKeyword("tag", reader =>
					country.Tag = new SingleString(reader).String
				);
				parser.RegisterKeyword("country_name", reader =>
					country.CountryName = CountryName.Parse(reader)
				);
				parser.RegisterKeyword("flag", reader =>
					country.Flag = new SingleString(reader).String
				);
				parser.RegisterKeyword("country_type", reader => {
					var countryTypeStr = new SingleString(reader).String;
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
					var capitalProvID = new SingleULong(reader).ULong;
					if (capitalProvID > 0) {
						country.Capital = capitalProvID;
					}
				});
				parser.RegisterKeyword("government_key", reader => {
					var governmentStr = new SingleString(reader).String;
					country.Government = governmentStr;
					// set government type
					if (monarchyGovernments.Contains(governmentStr))
						country.GovernmentType = GovernmentType.monarchy;
					else if (republicGovernments.Contains(governmentStr))
						country.GovernmentType = GovernmentType.republic;
					else if (tribalGovernments.Contains(governmentStr))
						country.GovernmentType = GovernmentType.tribal;
				});
				parser.RegisterKeyword("family", reader =>
					country.Families.Add(new SingleULong(reader).ULong, null)
				);
				parser.RegisterKeyword("minor_family", reader =>
					country.Families.Add(new SingleULong(reader).ULong, null)
				);
				parser.RegisterKeyword("monarch", reader =>
					country.Monarch = new SingleULong(reader).ULong
				);
				parser.RegisterRegex(monarchyLawRegexStr, reader =>
					country.monarchyLaws.Add(new SingleString(reader).String)
				);
				parser.RegisterRegex(republicLawRegexStr, reader =>
					country.republicLaws.Add(new SingleString(reader).String)
				);
				parser.RegisterRegex(tribalLawRegexStr, reader =>
					country.tribalLaws.Add(new SingleString(reader).String)
				);
				parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
			}
			public static Country Parse(BufferedReader reader, ulong countryID) {
				country = new Country(countryID);
				parser.ParseStream(reader);
				return country;
			}
		}
		public static Country Parse(BufferedReader reader, ulong countryID) {
			return CountryFactory.Parse(reader, countryID);
		}
	}
}
