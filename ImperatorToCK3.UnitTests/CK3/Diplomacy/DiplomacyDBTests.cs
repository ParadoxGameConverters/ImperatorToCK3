using commonItems;
using ImperatorToCK3.CK3.Diplomacy;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.Imperator.Countries;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace ImperatorToCK3.UnitTests.CK3.Diplomacy;

[Collection("Sequential")]
[CollectionDefinition("Sequential", DisableParallelization = true)]
public class DiplomacyDBTests {
	[Fact]
	public void ImportImperatorLeagues_SkipsMissingCountriesAndLogsWarning() {
		var db = new DiplomacyDB();
		var countries = new CountryCollection();

		// Only one country exists; league contains an unknown member
		var leagues = new List<List<ulong>> {
			new() { 1, 2 }
		};

		var output = new StringWriter();
		Console.SetOut(output);

		db.ImportImperatorLeagues(leagues, countries);

		Assert.Empty(db.Leagues);
		Assert.Contains("Member 2 of defensive league not found in countries!", output.ToString());
	}

	[Fact]
	public void ImportImperatorLeagues_SkipsLeaguesWithLessThanTwoAvailableMembers() {
		var db = new DiplomacyDB();
		var countries = new CountryCollection();
		var titles = new Title.LandedTitles();

		var title = titles.Add("k_test");
		countries.Add(new Country(1) { CK3Title = title });

		var leagues = new List<List<ulong>> {
			new() { 1 },
			new() { 1, 2 }
		};

		var output = new StringWriter();
		Console.SetOut(output);

		db.ImportImperatorLeagues(leagues, countries);

		// Only the first league had at least 2 members, but one member was missing, so none should be imported.
		Assert.Empty(db.Leagues);
		Assert.Contains("Not enough members in league to import it", output.ToString());
	}

	[Fact]
	public void ImportImperatorLeagues_AddsLeagueWithTwoValidMembers() {
		var db = new DiplomacyDB();
		var countries = new CountryCollection();
		var titles = new Title.LandedTitles();

		var titleA = titles.Add("k_a");
		var titleB = titles.Add("k_b");
		countries.Add(new Country(1) { CK3Title = titleA });
		countries.Add(new Country(2) { CK3Title = titleB });

		var leagues = new List<List<ulong>> {
			new() { 1, 2 }
		};

		db.ImportImperatorLeagues(leagues, countries);

		Assert.Single(db.Leagues);
		Assert.Equal(new[] { "k_a", "k_b" }, db.Leagues[0].Select(t => t.Id));
	}
}
