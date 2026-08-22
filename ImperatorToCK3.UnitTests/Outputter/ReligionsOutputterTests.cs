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

		var religionReader = new BufferedReader("faiths = { faith_test = {} }");
		religions.AddOrReplace(new Religion("christianity_test", religionReader, religions, new ColorFactory()));

		var siteWithCounty = new HolySite("site_county", new BufferedReader("county = c_test"), landedTitles, isFromConverter: true);
		var siteWithoutTitle = new HolySite("site_nowhere", new BufferedReader(string.Empty), landedTitles, isFromConverter: false);
		religions.HolySites.AddOrReplace(siteWithCounty);
		religions.HolySites.AddOrReplace(siteWithoutTitle);

		return (religions, new TestCK3LocDB());
	}

	[Fact]
	public async Task OutputReligionsAndHolySitesWritesBothFilesAndLoc() {
		var tempDir = CreateTempDir();
		try {
			Directory.CreateDirectory(Path.Combine(tempDir, "common", "religion", "holy_site_types"));
			Directory.CreateDirectory(Path.Combine(tempDir, "common", "religion", "religion_types"));

			var (religions, ck3LocDB) = CreateReligionCollectionWithSites();

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

			Assert.Equal("$c_test$", ck3LocDB.GetOrCreateLocBlock("holy_site_site_county_name")["english"]);
			Assert.Equal("Holy site", ck3LocDB.GetOrCreateLocBlock("holy_site_site_nowhere_name")["english"]);

			var effectLoc = ck3LocDB.GetOrCreateLocBlock("holy_site_site_county_effect_name")["english"];
			Assert.Contains("#weak ($holy_site_site_county_name$)#!", effectLoc, StringComparison.Ordinal);
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
