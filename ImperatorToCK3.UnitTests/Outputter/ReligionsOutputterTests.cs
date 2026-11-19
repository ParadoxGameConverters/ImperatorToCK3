using commonItems;
using commonItems.Colors;
using ImperatorToCK3.CK3.Religions;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.Outputter;
using ImperatorToCK3.UnitTests.TestHelpers;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace ImperatorToCK3.UnitTests.Outputter;

public class ReligionsOutputterTests {
	private static string CreateTempDir() {
		var dir = Path.Combine(Path.GetTempPath(), "ImperatorToCK3_UnitTests", "ReligionsOutputter", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(dir);
		return dir;
	}

	private static void TryDeleteDir(string dir) {
		try {
			if (Directory.Exists(dir)) {
				Directory.Delete(dir, recursive: true);
			}
		} catch {
			// Best-effort cleanup only.
		}
	}

	private static (ReligionCollection religions, TestCK3LocDB locDB) CreateReligionCollectionWithSites() {
		var landedTitles = new Title.LandedTitles();
		landedTitles.LoadTitles(new BufferedReader("""
			c_test = {
				b_test = { province = 1 }
			}
			"""), new ColorFactory());

		var religions = new ReligionCollection(landedTitles);

		var religion = new Religion("christianity_test", new BufferedReader("{}"), religions);
		religion.Faiths.AddOrReplace(new Faith("faith_test", new FaithData { ReligionId = "christianity_test" }, religion));
		religions.AddOrReplace(religion);

		var siteWithCounty = new HolySite("site_county", new BufferedReader("county = c_test"), landedTitles, isFromConverter: true);
		var siteWithoutTitle = new HolySite("site_nowhere", new BufferedReader(string.Empty), landedTitles, isFromConverter: false);
		religions.HolySites.AddOrReplace(siteWithCounty);
		religions.HolySites.AddOrReplace(siteWithoutTitle);

		return (religions, new TestCK3LocDB());
	}

	private static (ReligionCollection religions, TestCK3LocDB locDB) CreateReligionCollectionWithRites() {
		var (religions, locDB) = CreateReligionCollectionWithSites();
		var faith = religions.GetFaith("faith_test");
		Assert.NotNull(faith);

		religions.Rites.AddOrReplace(Rite.CreateMainlineRite(faith));

		var gameRite = new Rite("game_rite", new BufferedReader("""
			{
				faith = faith_test
				color = rgb { 1 2 3 }
				doctrines = { doctrine_test }
				cultures = { roman }
			}
			"""), religions, new ColorFactory());
		religions.Rites.AddOrReplace(gameRite);

		var faithlessRite = new Rite("faithless_rite", new BufferedReader("""
			{
				color = rgb { 4 5 6 }
			}
			"""), religions, new ColorFactory());
		religions.Rites.AddOrReplace(faithlessRite);

		return (religions, locDB);
	}

	[Fact]
	public async Task OutputReligionsAndHolySitesWritesBothFilesAndLoc() {
		var tempDir = CreateTempDir();
		try {
			Directory.CreateDirectory(Path.Combine(tempDir, "common", "religion", "holy_site_types"));
			Directory.CreateDirectory(Path.Combine(tempDir, "common", "religion", "religion_types"));

			var (religions, ck3LocDB) = CreateReligionCollectionWithSites();
			religions.ConverterFaithIds.Add("faith_test");

			await ReligionsOutputter.OutputReligionsAndHolySites(tempDir, religions, ck3LocDB);

			var holySitesText = await File.ReadAllTextAsync(
				Path.Combine(tempDir, "common", "religion", "holy_site_types", "all_holy_sites.txt"),
				TestContext.Current.CancellationToken);
			Assert.Contains("site_county=", holySitesText, StringComparison.Ordinal);
			Assert.Contains("county = c_test", holySitesText, StringComparison.Ordinal);
			Assert.Contains("site_nowhere=", holySitesText, StringComparison.Ordinal);

			var religionsText = await File.ReadAllTextAsync(
				Path.Combine(tempDir, "common", "religion", "religion_types", "IRtoCK3_all_religions.txt"),
				TestContext.Current.CancellationToken);
			Assert.Contains("christianity_test=", religionsText, StringComparison.Ordinal);

			var faithsText = await File.ReadAllTextAsync(
				Path.Combine(tempDir, "common", "religion", "faith_types", "IRtoCK3_all_faiths.txt"),
				TestContext.Current.CancellationToken);
			Assert.Contains("faith_test=", faithsText, StringComparison.Ordinal);
			Assert.Contains("faith_details = {", faithsText, StringComparison.Ordinal);
			Assert.Contains("religion = christianity_test", faithsText, StringComparison.Ordinal);

			var faithHistoryText = await File.ReadAllTextAsync(
				Path.Combine(tempDir, "history", "faiths", "IRtoCK3_converter_faiths.txt"),
				TestContext.Current.CancellationToken);
			Assert.Contains("faith_test={", faithHistoryText, StringComparison.Ordinal);
			Assert.Contains("created = yes", faithHistoryText, StringComparison.Ordinal);

			Assert.Equal("$c_test$", ck3LocDB.GetOrCreateLocBlock("holy_site_site_county_name")["english"]);
			Assert.Equal("Holy site", ck3LocDB.GetOrCreateLocBlock("holy_site_site_nowhere_name")["english"]);

			var effectLoc = ck3LocDB.GetOrCreateLocBlock("holy_site_site_county_effects")["english"];
			Assert.Contains("#weak ($holy_site_site_county_name$)#!", effectLoc, StringComparison.Ordinal);
		} finally {
			TryDeleteDir(tempDir);
		}
	}

	[Fact]
	public async Task OutputReligionsAndHolySitesWritesAllRites() {
		var tempDir = CreateTempDir();
		try {
			var (religions, locDB) = CreateReligionCollectionWithRites();

			await ReligionsOutputter.OutputReligionsAndHolySites(tempDir, religions, locDB);

			var ritesText = await File.ReadAllTextAsync(
				Path.Combine(tempDir, "common", "religion", "rite_types", "IRtoCK3_all_rites.txt"),
				TestContext.Current.CancellationToken);
			Assert.Contains("faith_test=", ritesText, StringComparison.Ordinal);
			Assert.Contains("game_rite=", ritesText, StringComparison.Ordinal);
			Assert.Contains("faithless_rite=", ritesText, StringComparison.Ordinal);
			Assert.Contains("cultures = { roman }", ritesText, StringComparison.Ordinal);
		} finally {
			TryDeleteDir(tempDir);
		}
	}

	[Fact]
	public async Task OutputHolySitesKeepsExistingLocalization() {
		var tempDir = CreateTempDir();
		try {
			Directory.CreateDirectory(Path.Combine(tempDir, "common", "religion", "holy_site_types"));
			Directory.CreateDirectory(Path.Combine(tempDir, "common", "religion", "religion_types"));

			var (religions, ck3LocDB) = CreateReligionCollectionWithSites();

			const string customName = "Custom site name";
			ck3LocDB.GetOrCreateLocBlock("holy_site_site_county_name")["english"] = customName;

			await ReligionsOutputter.OutputReligionsAndHolySites(tempDir, religions, ck3LocDB);

			Assert.Equal(customName, ck3LocDB.GetOrCreateLocBlock("holy_site_site_county_name")["english"]);
			Assert.NotEqual(customName, ck3LocDB.GetOrCreateLocBlock("holy_site_site_county_name")["french"]);
			Assert.Equal("$c_test$", ck3LocDB.GetOrCreateLocBlock("holy_site_site_county_name")["french"]);
		} finally {
			TryDeleteDir(tempDir);
		}
	}
}
