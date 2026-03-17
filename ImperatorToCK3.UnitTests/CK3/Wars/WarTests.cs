using commonItems;
using ImperatorToCK3.CK3.Provinces;
using ImperatorToCK3.CK3.Titles;
using CK3War = ImperatorToCK3.CK3.Wars.War;
using ImperatorWar = ImperatorToCK3.Imperator.Diplomacy.War;
using ImperatorToCK3.Imperator.Countries;
using ImperatorToCK3.Imperator.States;
using ImperatorToCK3.Mappers.Province;
using ImperatorToCK3.Mappers.War;
using System.IO;
using System.Reflection;
using commonItems.Exceptions;
using Xunit;

namespace ImperatorToCK3.UnitTests.CK3.Wars;

[Collection("Sequential")]
[CollectionDefinition("Sequential", DisableParallelization = true)]
public class WarTests {
	[Fact]
	public void Constructor_ThrowsWhenNoValidAttackers() {
		var war = ImperatorWar.Parse(new BufferedReader("start_date = 100.1.1\nattacker = 1\nindependence = { type = independence }"));
		var mapperFile = "TestFiles/configurables/temp_wargoal_map_wartest.txt";
		File.WriteAllText(mapperFile, "link = { ck3 = cb ir = independence }");
		var warMapper = new WarMapper(mapperFile);

		var countries = new CountryCollection {
			new Country(1)
		};

		var provinceMapper = new ProvinceMapper();
		var states = new StateCollection();
		var provinces = new ProvinceCollection();
		var titles = new Title.LandedTitles();

		var bookmarkDate = new Date(1100, 1, 1);

		var ex = Assert.Throws<ConverterException>(() => new CK3War(war, warMapper, provinceMapper, countries, states, provinces, titles, bookmarkDate));
		Assert.Contains("War has no valid attackers", ex.Message);
	}

	[Fact]
	public void Constructor_PopulatesAttackersClaimantAndCasusBelli() {
		var war = ImperatorWar.Parse(new BufferedReader("start_date = 100.1.1\nattacker = 1\ndefender = 2\nindependence = { type = independence }"));
		var mapperFile = "TestFiles/configurables/temp_wargoal_map_wartest.txt";
		File.WriteAllText(mapperFile, "link = { ck3 = cb_independence ir = independence }");
		var warMapper = new WarMapper(mapperFile);

		var countries = new CountryCollection();
		var titles = new Title.LandedTitles();

		var attackerTitle = titles.Add("k_attacker");
		SetTitleHolder(attackerTitle, "holder1", new Date(1000, 1, 1));
		var attackerCountry = new Country(1) { CK3Title = attackerTitle };
		countries.Add(attackerCountry);

		var defenderTitle = titles.Add("k_defender");
		SetTitleHolder(defenderTitle, "holder2", new Date(1000, 1, 1));
		var defenderCountry = new Country(2) { CK3Title = defenderTitle };
		countries.Add(defenderCountry);

		var provinceMapper = new ProvinceMapper();
		var states = new StateCollection();
		var provinces = new ProvinceCollection();

		var bookmarkDate = new Date(1100, 1, 1);
		var createdWar = new CK3War(war, warMapper, provinceMapper, countries, states, provinces, titles, bookmarkDate);

		Assert.Equal("holder1", Assert.Single(createdWar.Attackers));
		Assert.Equal("holder1", createdWar.Claimant);
		Assert.Equal("cb_independence", createdWar.CasusBelli);
		Assert.Equal(bookmarkDate.ChangeByDays(1), createdWar.EndDate);
	}

	private static void SetTitleHolder(Title title, string holderId, Date date) {
		// Title stores its history in a private field/property created by the source generator.
		var historyProperty = typeof(Title).GetProperty("History", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
		var history = historyProperty?.GetValue(title);
		var addMethod = history?.GetType().GetMethod("AddFieldValue", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		addMethod?.Invoke(history, [date, "holder", "holder", holderId]);
	}
}
