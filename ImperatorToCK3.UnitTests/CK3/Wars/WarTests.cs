using commonItems;
using commonItems.Colors;
using commonItems.Mods;
using ImperatorToCK3.CK3.Provinces;
using ImperatorToCK3.CK3.Titles;
using CK3War = ImperatorToCK3.CK3.Wars.War;
using ImperatorWar = ImperatorToCK3.Imperator.Diplomacy.War;
using ImperatorToCK3.CommonUtils.Map;
using ImperatorToCK3.Imperator.Countries;
using ImperatorToCK3.Imperator.Geography;
using ImperatorToCK3.Imperator.States;
using ImperatorToCK3.Mappers.Province;
using ImperatorToCK3.Mappers.War;
using System;
using System.Collections.Generic;
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

	[Fact]
	public void Constructor_ThrowsWhenTargetedStateNotFound() {
		var war = ImperatorWar.Parse(new BufferedReader(
			"start_date = 100.1.1\nattacker = 1\ntake_province = { type = take_province state = 999 }"));
		var warMapper = MakeWarMapper("link = { ck3 = cb_take_province ir = take_province }");

		var titles = new Title.LandedTitles();
		var attackerTitle = titles.Add("k_attacker");
		SetTitleHolder(attackerTitle, "holder1", new Date(1000, 1, 1));
		var countries = new CountryCollection {
			new Country(1) { CK3Title = attackerTitle }
		};

		var ex = Assert.Throws<ConverterException>(() => new CK3War(
			war, warMapper, new ProvinceMapper(), countries, new StateCollection(),
			new ProvinceCollection(), titles, new Date(1100, 1, 1)));
		Assert.Contains("targeted state", ex.Message);
	}

	[Fact]
	public void Constructor_AddsTargetedStateCountiesToTargetedTitles() {
		var war = ImperatorWar.Parse(new BufferedReader(
			"start_date = 100.1.1\nattacker = 1\ndefender = 2\ntake_province = { type = take_province state = 1 }"));
		var warMapper = MakeWarMapper("link = { ck3 = cb_take_province ir = take_province }");

		const string imperatorRoot = "TestFiles/Imperator/game";
		var irModFS = new ModFilesystem(imperatorRoot, Array.Empty<Mod>());
		var irProvinces = new ImperatorToCK3.Imperator.Provinces.ProvinceCollection();
		var area = new Area("test_area", new BufferedReader("provinces = { 1 2 }"), irProvinces);
		var countries = new CountryCollection { new Country(1), new Country(2) };
		var states = new StateCollection();
		states.Add(new State(1, new StateData { Area = area, CapitalProvinceId = 1, Country = countries[1] }));
		irProvinces.LoadProvinces(new BufferedReader("1={ state=1 } 2={ state=1 }"),
			states, countries, new MapData(irModFS));

		var mapperFile = "TestFiles/configurables/temp_province_map_wartest.txt";
		File.WriteAllText(mapperFile, "test_version = { link={imp=1 ck3=10} link={imp=2 ck3=11} }");
		var provinceMapper = new ProvinceMapper();
		provinceMapper.LoadMappings(mapperFile);

		var titles = new Title.LandedTitles();
		titles.LoadTitles(new BufferedReader(
			"c_target = { b_target1 = { province = 10 } b_target2 = { province = 11 } }"), new ColorFactory());
		var attackerTitle = titles.Add("k_attacker");
		SetTitleHolder(attackerTitle, "holder1", new Date(1000, 1, 1));
		var defenderTitle = titles.Add("k_defender");
		SetTitleHolder(defenderTitle, "holder2", new Date(1000, 1, 1));
		countries[1].CK3Title = attackerTitle;
		countries[2].CK3Title = defenderTitle;

		var bookmarkDate = new Date(1100, 1, 1);
		var createdWar = new CK3War(war, warMapper, provinceMapper, countries, states,
			new ProvinceCollection(), titles, bookmarkDate);

		Assert.Equal("c_target", Assert.Single(createdWar.TargetedTitles));
		Assert.Equal("holder2", Assert.Single(createdWar.Defenders));
	}

	[Fact]
	public void Constructor_SkipsDefendersWithoutTitleOrHolder() {
		var war = ImperatorWar.Parse(new BufferedReader(
			"start_date = 100.1.1\nattacker = 1\ndefender = 2\ndefender = 3\ndefender = 4\ndefender = 5\nindependence = { type = independence }"));
		var warMapper = MakeWarMapper("link = { ck3 = cb_independence ir = independence }");

		var titles = new Title.LandedTitles();
		var attackerTitle = titles.Add("k_attacker");
		SetTitleHolder(attackerTitle, "holder1", new Date(1000, 1, 1));
		var holderlessTitle = titles.Add("k_holderless");
		var defenderTitle = titles.Add("k_defender");
		SetTitleHolder(defenderTitle, "holder2", new Date(1000, 1, 1));
		var secondDefenderTitle = titles.Add("k_defender2");
		SetTitleHolder(secondDefenderTitle, "holder3", new Date(1000, 1, 1));
		var countries = new CountryCollection {
			new Country(1) { CK3Title = attackerTitle },
			new Country(2), // no CK3 title
			new Country(3) { CK3Title = holderlessTitle }, // title without holder
			new Country(4) { CK3Title = defenderTitle },
			new Country(5) { CK3Title = secondDefenderTitle }
		};

		var bookmarkDate = new Date(1100, 1, 1);
		var createdWar = new CK3War(war, warMapper, new ProvinceMapper(), countries,
			new StateCollection(), new ProvinceCollection(), titles, bookmarkDate);

		Assert.Equal(new List<string> { "holder2", "holder3" }, createdWar.Defenders);
	}

	private static WarMapper MakeWarMapper(string mapContent) {
		var mapperFile = "TestFiles/configurables/temp_wargoal_map_wartest.txt";
		File.WriteAllText(mapperFile, mapContent);
		return new WarMapper(mapperFile);
	}

	private static void SetTitleHolder(Title title, string holderId, Date date) {		// Title stores its history in a private field/property created by the source generator.
		var historyProperty = typeof(Title).GetProperty("History", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
		var history = historyProperty?.GetValue(title);
		var addMethod = history?.GetType().GetMethod("AddFieldValue", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		addMethod?.Invoke(history, [date, "holder", "holder", holderId]);
	}
}
