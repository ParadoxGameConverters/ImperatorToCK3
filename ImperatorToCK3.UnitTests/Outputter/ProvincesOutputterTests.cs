using commonItems;
using commonItems.Colors;
using ImperatorToCK3.CK3.Provinces;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.Outputter;
using ImperatorToCK3.UnitTests.TestHelpers;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace ImperatorToCK3.UnitTests.Outputter;

[Collection("Sequential")]
[CollectionDefinition("Sequential", DisableParallelization = true)]
public class ProvincesOutputterTests {
	private static readonly ColorFactory colorFactory = new();

	private static string CreateTempDir() {
		var dir = Path.Combine(Path.GetTempPath(), "ImperatorToCK3_UnitTests", "ProvincesOutputter", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(dir);
		return dir;
	}

	private static void TryDeleteDir(string dir) {
		try {
			if (Directory.Exists(dir)) {
				Directory.Delete(dir, recursive: true);
			}
		} catch {
			// Best effort.
		}
	}

	private static async Task<string> ReadText(string path) {
		var text = await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken);
		return TextTestUtils.NormalizeNewlines(text);
	}

	private static void EnsureProvincesOutputDirectories(string outputModPath) {
		Directory.CreateDirectory(Path.Combine(outputModPath, "history", "provinces"));
		Directory.CreateDirectory(Path.Combine(outputModPath, "history", "province_mapping"));
	}

	[Fact]
	public async Task OutputProvinces_WritesProvincesGroupedByDeJureKingdoms_NoFallbackOrMappingWhenAllCovered() {
		var tempRoot = CreateTempDir();
		try {
			var outputModPath = Path.Combine(tempRoot, "outputMod");
			EnsureProvincesOutputDirectories(outputModPath);

			// Build titles: two kingdoms, each with one duchy and one county with two baronies
			// k1: d1 -> c1 (provinces 10 capital, 11 non-capital)
			// k2: d2 -> c2 (provinces 30 capital, 31 non-capital)
			// Also add a county without capital to test Where(id is not null) filtering
			var titles = new Title.LandedTitles();

			var c1Reader = new BufferedReader("b_c1_1 = { province=10 } b_c1_2 = { province=11 }");
			var c1 = titles.Add("c_county1");
			c1.LoadTitles(c1Reader, colorFactory);
			var d1 = titles.Add("d_duchy1");
			c1.DeJureLiege = d1;
			var k1 = titles.Add("k_kingdom1");
			d1.DeJureLiege = k1;

			var c2Reader = new BufferedReader("b_c2_1 = { province=30 } b_c2_2 = { province=31 }");
			var c2 = titles.Add("c_county2");
			c2.LoadTitles(c2Reader, colorFactory);
			var d2 = titles.Add("d_duchy2");
			c2.DeJureLiege = d2;
			var k2 = titles.Add("k_kingdom2");
			d2.DeJureLiege = k2;

			// County without barony => CapitalBaronyProvinceId == null => Where clause false branch
			var cNoCap = titles.Add("c_county_nocap");
			var dNoCap = titles.Add("d_duchy_nocap");
			cNoCap.DeJureLiege = dNoCap;
			// Do NOT attach dNoCap to any kingdom, but it still counts as de jure duchy.
			// However k1/k2 still cover the test provinces; this county just ensures null capital case.

			var provinces = new ProvinceCollection();
			// Provinces 10 and 30 are county capitals (true), 11 and 31 are not (false)
			// Give them cultures to verify county-capital filtering in output
			var p10 = new Province(10, new BufferedReader("culture=roman holding=castle_holding"));
			var p11 = new Province(11, new BufferedReader("culture=greek holding=city_holding"));
			var p30 = new Province(30, new BufferedReader("culture=roman holding=castle_holding"));
			var p31 = new Province(31, new BufferedReader("culture=greek holding=city_holding"));
			provinces.AddOrReplace(p10);
			provinces.AddOrReplace(p11);
			provinces.AddOrReplace(p30);
			provinces.AddOrReplace(p31);

			await ProvincesOutputter.OutputProvinces(outputModPath, provinces, titles);

			// Verify kingdom files exist and contain correct provinces
			var k1Path = Path.Combine(outputModPath, "history", "provinces", "k_kingdom1.txt");
			var k2Path = Path.Combine(outputModPath, "history", "provinces", "k_kingdom2.txt");
			Assert.True(File.Exists(k1Path));
			Assert.True(File.Exists(k2Path));

			var k1Text = await ReadText(k1Path);
			// k1 should contain 10 (capital) with culture, and 11 without culture
			Assert.Contains("10={", k1Text);
			Assert.Contains("11={", k1Text);
			Assert.DoesNotContain("30={", k1Text);
			Assert.DoesNotContain("31={", k1Text);
			// County capital 10 retains culture
			Assert.Contains("culture = roman", k1Text);
			// For kingdom file, both provinces are written; check that 11's block does NOT contain culture
			// We can check by ensuring greek not present (since 11 is non-capital and culture removed) while roman is
			// However both 10 and 11 use different cultures; k1 has 10 roman, 11 greek. Since 11 greek should be stripped, greek must not appear in k1Text
			Assert.DoesNotContain("greek", k1Text);

			var k2Text = await ReadText(k2Path);
			Assert.Contains("30={", k2Text);
			Assert.Contains("31={", k2Text);
			Assert.DoesNotContain("10={", k2Text);
			Assert.DoesNotContain("11={", k2Text);
			Assert.Contains("culture = roman", k2Text);
			Assert.DoesNotContain("greek", k2Text);

			// No fallback file or mapping file should be created when all provinces were covered
			var onlyDuchyPath = Path.Combine(outputModPath, "history", "provinces", "onlyDeJureDuchy.txt");
			var mappingPath = Path.Combine(outputModPath, "history", "province_mapping", "province_mapping.txt");
			Assert.False(File.Exists(onlyDuchyPath));
			Assert.False(File.Exists(mappingPath));
		} finally {
			TryDeleteDir(tempRoot);
		}
	}

	[Fact]
	public async Task OutputProvinces_FallsBackToDuchiesAndWritesMappingIncludingNullBaseSkipping() {
		var tempRoot = CreateTempDir();
		try {
			var outputModPath = Path.Combine(tempRoot, "outputMod");
			EnsureProvincesOutputDirectories(outputModPath);

			var titles = new Title.LandedTitles();

			// Kingdom k1 with duchy d1 and county c1 covering provinces 10 (capital) and 11 (non-capital)
			var c1Reader = new BufferedReader("b_c1_1 = { province=10 } b_c1_2 = { province=11 }");
			var c1 = titles.Add("c_county1");
			c1.LoadTitles(c1Reader, colorFactory);
			var d1 = titles.Add("d_duchy1");
			c1.DeJureLiege = d1;
			var k1 = titles.Add("k_kingdom1");
			d1.DeJureLiege = k1;

			// Orphan duchy d_orphan with county c_orphan covering 20 (capital) and 21 (non-capital), no kingdom liege
			var cOrphanReader = new BufferedReader("b_cOrphan_1 = { province=20 } b_cOrphan_2 = { province=21 }");
			var cOrphan = titles.Add("c_county_orphan");
			cOrphan.LoadTitles(cOrphanReader, colorFactory);
			var dOrphan = titles.Add("d_orphan_duchy");
			cOrphan.DeJureLiege = dOrphan;

			// Extra duchy with province 999 that will NOT be in ProvinceCollection -> tests sb.Length>0 false
			var cExtraReader = new BufferedReader("b_extra = { province=999 }");
			var cExtra = titles.Add("c_county_extra");
			cExtra.LoadTitles(cExtraReader, colorFactory);
			var dExtra = titles.Add("d_extra_duchy");
			cExtra.DeJureLiege = dExtra;

			// Province collection: 10,11 in kingdom; 20,21 in orphan duchy; 40 with base, 41 without base are orphans
			var provinces = new ProvinceCollection();
			var p10 = new Province(10, new BufferedReader("culture=roman holding=castle_holding"));
			var p11 = new Province(11, new BufferedReader("culture=greek holding=city_holding"));
			var p20 = new Province(20, new BufferedReader("culture=roman holding=castle_holding"));
			var p21 = new Province(21, new BufferedReader("culture=greek holding=city_holding"));

			// Orphan with base province mapping
			var sourceProv = new Province(99, new BufferedReader("culture=roman faith=orthodox terrain=plains"));
			var p40 = new Province(40); // empty history then copy
			p40.CopyEntriesFromProvince(sourceProv); // BaseProvinceId == 99, will be written to mapping

			// Orphan without base
			var p41 = new Province(41, new BufferedReader("holding=castle_holding"));

			provinces.AddOrReplace(p10);
			provinces.AddOrReplace(p11);
			provinces.AddOrReplace(p20);
			provinces.AddOrReplace(p21);
			provinces.AddOrReplace(p40);
			provinces.AddOrReplace(p41);

			await ProvincesOutputter.OutputProvinces(outputModPath, provinces, titles);

			// Kingdom file should contain only 10 and 11
			var k1Path = Path.Combine(outputModPath, "history", "provinces", "k_kingdom1.txt");
			Assert.True(File.Exists(k1Path));
			var k1Text = await ReadText(k1Path);
			Assert.Contains("10={", k1Text);
			Assert.Contains("11={", k1Text);
			Assert.DoesNotContain("20={", k1Text);
			Assert.DoesNotContain("40={", k1Text);
			Assert.DoesNotContain("greek", k1Text); // non-capital 11 stripped

			// OnlyDeJureDuchy file should exist and contain d_orphan provinces 20,21 with comment lines
			var onlyDuchyPath = Path.Combine(outputModPath, "history", "provinces", "onlyDeJureDuchy.txt");
			Assert.True(File.Exists(onlyDuchyPath));
			var onlyDuchyText = await ReadText(onlyDuchyPath);
			// Should contain comment for d_orphan_duchy
			Assert.Contains("# d_orphan_duchy", onlyDuchyText);
			Assert.Contains("20={", onlyDuchyText);
			Assert.Contains("21={", onlyDuchyText);
			// 20 is capital => retains culture, 21 non-capital => culture stripped => no greek
			Assert.Contains("culture = roman", onlyDuchyText);
			Assert.DoesNotContain("greek", onlyDuchyText);
			// Should NOT contain 10,11 because they were already outputted (alreadyOutputted.Contains true branch)
			Assert.DoesNotContain("10={", onlyDuchyText);
			Assert.DoesNotContain("11={", onlyDuchyText);
			// Should NOT contain extra duchy's comment because its province 999 not in collection (sb.Length false branch)
			Assert.DoesNotContain("# d_extra_duchy", onlyDuchyText);
			Assert.DoesNotContain("999", onlyDuchyText);
			// Orphans 40,41 should not be in duchy file because DuchyContainsProvince false branch
			Assert.DoesNotContain("40={", onlyDuchyText);
			Assert.DoesNotContain("41={", onlyDuchyText);

			// Province mapping file should exist and contain only 40 = 99
			var mappingPath = Path.Combine(outputModPath, "history", "province_mapping", "province_mapping.txt");
			Assert.True(File.Exists(mappingPath));
			var mappingText = await ReadText(mappingPath);
			// Contains mapping for 40
			Assert.Contains("40 = 99", mappingText);
			// Should NOT contain 41 because base is null (baseProvId is null branch)
			Assert.DoesNotContain("41 =", mappingText);
			// Should NOT contain already outputted 10,11,20,21 (alreadyOutputted.Contains true branch in mapping)
			Assert.DoesNotContain("10 =", mappingText);
			Assert.DoesNotContain("20 =", mappingText);
		} finally {
			TryDeleteDir(tempRoot);
		}
	}

	[Fact]
	public async Task OutputProvinces_DoesNotCreateMappingWhenDuchiesCoverAllRemaining() {
		var tempRoot = CreateTempDir();
		try {
			var outputModPath = Path.Combine(tempRoot, "outputMod");
			EnsureProvincesOutputDirectories(outputModPath);

			var titles = new Title.LandedTitles();

			// Kingdom covers 10,11
			var c1Reader = new BufferedReader("b_c1_1 = { province=10 }");
			var c1 = titles.Add("c_county1");
			c1.LoadTitles(c1Reader, colorFactory);
			var d1 = titles.Add("d_duchy1");
			c1.DeJureLiege = d1;
			var k1 = titles.Add("k_kingdom1");
			d1.DeJureLiege = k1;

			// Orphan duchy covers 20
			var cOrphanReader = new BufferedReader("b_cOrphan = { province=20 }");
			var cOrphan = titles.Add("c_county_orphan");
			cOrphan.LoadTitles(cOrphanReader, colorFactory);
			var dOrphan = titles.Add("d_orphan_duchy");
			cOrphan.DeJureLiege = dOrphan;

			var provinces = new ProvinceCollection();
			var p10 = new Province(10, new BufferedReader("holding=castle_holding"));
			var p20 = new Province(20, new BufferedReader("holding=castle_holding"));
			provinces.AddOrReplace(p10);
			provinces.AddOrReplace(p20);

			await ProvincesOutputter.OutputProvinces(outputModPath, provinces, titles);

			// Both provinces should be covered: 10 via kingdom, 20 via duchy
			Assert.True(File.Exists(Path.Combine(outputModPath, "history", "provinces", "k_kingdom1.txt")));
			var onlyDuchyPath = Path.Combine(outputModPath, "history", "provinces", "onlyDeJureDuchy.txt");
			Assert.True(File.Exists(onlyDuchyPath));
			var onlyDuchyText = await ReadText(onlyDuchyPath);
			Assert.Contains("20={", onlyDuchyText);

			// No remaining provinces => second if false => mapping file should NOT be created
			var mappingPath = Path.Combine(outputModPath, "history", "province_mapping", "province_mapping.txt");
			Assert.False(File.Exists(mappingPath));
		} finally {
			TryDeleteDir(tempRoot);
		}
	}

	[Fact]
	public async Task OutputProvinces_HandlesNoKingdomsAndEmptyDuchyMatchesCreatesEmptyFilesAndMapping() {
		var tempRoot = CreateTempDir();
		try {
			var outputModPath = Path.Combine(tempRoot, "outputMod");
			EnsureProvincesOutputDirectories(outputModPath);

			var titles = new Title.LandedTitles();
			// No kingdoms at all -> GetDeJureKingdoms empty
			// One duchy with province 999 not in collection -> GetDeJureDuchies returns it, but DuchyContainsProvince always false
			var cExtraReader = new BufferedReader("b_extra = { province=999 }");
			var cExtra = titles.Add("c_county_extra");
			cExtra.LoadTitles(cExtraReader, colorFactory);
			var dExtra = titles.Add("d_extra_duchy");
			cExtra.DeJureLiege = dExtra;

			var provinces = new ProvinceCollection();
			var source = new Province(99, new BufferedReader("culture=roman"));
			var orphanWithBase = new Province(40);
			orphanWithBase.CopyEntriesFromProvince(source);
			var orphanWithoutBase = new Province(41, new BufferedReader("holding=none"));
			provinces.AddOrReplace(orphanWithBase);
			provinces.AddOrReplace(orphanWithoutBase);

			await ProvincesOutputter.OutputProvinces(outputModPath, provinces, titles);

			// Since no kingdom, no kingdom files should be created (though Parallel.ForEach no iteration)
			// Directory should remain but no k_*.txt files
			var kingdomFiles = Directory.GetFiles(Path.Combine(outputModPath, "history", "provinces"), "k_*.txt");
			Assert.Empty(kingdomFiles);

			// OnlyDeJureDuchy file is created even if empty (first if entered because 0 != 2)
			var onlyDuchyPath = Path.Combine(outputModPath, "history", "provinces", "onlyDeJureDuchy.txt");
			Assert.True(File.Exists(onlyDuchyPath));
			var onlyDuchyText = await ReadText(onlyDuchyPath);
			Assert.True(string.IsNullOrWhiteSpace(onlyDuchyText)); // empty because d_extra had no match (sb.Length false)

			// Mapping file should contain only 40 = 99
			var mappingPath = Path.Combine(outputModPath, "history", "province_mapping", "province_mapping.txt");
			Assert.True(File.Exists(mappingPath));
			var mappingText = await ReadText(mappingPath);
			Assert.Contains("40 = 99", mappingText);
			Assert.DoesNotContain("41 =", mappingText);
		} finally {
			TryDeleteDir(tempRoot);
		}
	}

	[Fact]
	public async Task OutputProvinces_HandlesEmptyProvinceCollection() {
		var tempRoot = CreateTempDir();
		try {
			var outputModPath = Path.Combine(tempRoot, "outputMod");
			EnsureProvincesOutputDirectories(outputModPath);

			var titles = new Title.LandedTitles();
			// Add a kingdom to verify empty province handling creates empty kingdom file but no fallback
			var cReader = new BufferedReader("b_c = { province=10 }");
			var c = titles.Add("c_county1");
			c.LoadTitles(cReader, colorFactory);
			var d = titles.Add("d_duchy1");
			c.DeJureLiege = d;
			var k = titles.Add("k_kingdom1");
			d.DeJureLiege = k;

			var provinces = new ProvinceCollection(); // empty

			await ProvincesOutputter.OutputProvinces(outputModPath, provinces, titles);

			// Kingdom file should exist but be empty (sb never appended)
			var kPath = Path.Combine(outputModPath, "history", "provinces", "k_kingdom1.txt");
			Assert.True(File.Exists(kPath));
			var kText = await ReadText(kPath);
			Assert.True(string.IsNullOrWhiteSpace(kText));

			// No onlyDeJureDuchy or mapping because alreadyOutputted.Count (0) == provinces.Count (0) => both ifs false
			Assert.False(File.Exists(Path.Combine(outputModPath, "history", "provinces", "onlyDeJureDuchy.txt")));
			Assert.False(File.Exists(Path.Combine(outputModPath, "history", "province_mapping", "province_mapping.txt")));
		} finally {
			TryDeleteDir(tempRoot);
		}
	}

	[Fact]
	public async Task OutputProvinces_CountyCapitalBranchWhereFilterAndContainsBothWays() {
		var tempRoot = CreateTempDir();
		try {
			var outputModPath = Path.Combine(tempRoot, "outputMod");
			EnsureProvincesOutputDirectories(outputModPath);

			var titles = new Title.LandedTitles();

			// County with capital (province 10) and non-capital 11
			var c1Reader = new BufferedReader("b_c1_1 = { province=10 } b_c1_2 = { province=11 }");
			var c1 = titles.Add("c_county1");
			c1.LoadTitles(c1Reader, colorFactory);
			var d1 = titles.Add("d_duchy1");
			c1.DeJureLiege = d1;
			var k1 = titles.Add("k_kingdom1");
			d1.DeJureLiege = k1;

			// County with capital 20
			var c2Reader = new BufferedReader("b_c2 = { province=20 }");
			var c2 = titles.Add("c_county2");
			c2.LoadTitles(c2Reader, colorFactory);
			var d2 = titles.Add("d_duchy2");
			c2.DeJureLiege = d2;
			d2.DeJureLiege = k1; // also under same kingdom

			// County without baronies -> capital null -> Where id is not null false branch
			var cNoCap = titles.Add("c_county_nocap");
			var dNoCap = titles.Add("d_duchy_nocap");
			cNoCap.DeJureLiege = dNoCap;
			dNoCap.DeJureLiege = k1;

			// Province 10 (capital true), 11 (capital false), 20 (capital true) all under same kingdom
			var provinces = new ProvinceCollection();
			var p10 = new Province(10, new BufferedReader("culture=roman holding=castle_holding faith=orthodox"));
			var p11 = new Province(11, new BufferedReader("culture=greek holding=city_holding faith=catholic"));
			var p20 = new Province(20, new BufferedReader("culture=roman holding=castle_holding"));
			provinces.AddOrReplace(p10);
			provinces.AddOrReplace(p11);
			provinces.AddOrReplace(p20);

			await ProvincesOutputter.OutputProvinces(outputModPath, provinces, titles);

			var kPath = Path.Combine(outputModPath, "history", "provinces", "k_kingdom1.txt");
			var kText = await ReadText(kPath);
			// Verify 10 and 20 retain culture/faith (capital), 11 stripped
			Assert.Contains("10={", kText);
			Assert.Contains("20={", kText);
			Assert.Contains("11={", kText);
			// Count occurrences of culture = roman (should be 2 for 10 and 20)
			var romanCount = kText.Split("culture = roman").Length - 1;
			Assert.Equal(2, romanCount);
			// greek should not appear (since 11 non-capital faith/culture removed)
			Assert.DoesNotContain("greek", kText);
			Assert.DoesNotContain("catholic", kText);
			// But orthodox should appear once (for 10)
			Assert.Contains("orthodox", kText);
		} finally {
			TryDeleteDir(tempRoot);
		}
	}

	[Fact]
	public async Task OutputProvinces_WritesEmptyMappingFileWhenRemainingOrphansHaveNoBase() {
		var tempRoot = CreateTempDir();
		try {
			var outputModPath = Path.Combine(tempRoot, "outputMod");
			EnsureProvincesOutputDirectories(outputModPath);

			var titles = new Title.LandedTitles();
			// No kingdoms, one orphan duchy that doesn't match
			var cExtraReader = new BufferedReader("b_extra = { province=999 }");
			var cExtra = titles.Add("c_county_extra");
			cExtra.LoadTitles(cExtraReader, colorFactory);
			var dExtra = titles.Add("d_extra_duchy");
			cExtra.DeJureLiege = dExtra;

			var provinces = new ProvinceCollection();
			// Two orphans without base province -> BaseProvinceId is null branch for both
			var p40 = new Province(40, new BufferedReader("holding=castle_holding"));
			var p41 = new Province(41, new BufferedReader("holding=city_holding"));
			provinces.AddOrReplace(p40);
			provinces.AddOrReplace(p41);

			await ProvincesOutputter.OutputProvinces(outputModPath, provinces, titles);

			var mappingPath = Path.Combine(outputModPath, "history", "province_mapping", "province_mapping.txt");
			Assert.True(File.Exists(mappingPath));
			var mappingText = await ReadText(mappingPath);
			// No lines because both have null base
			Assert.True(string.IsNullOrWhiteSpace(mappingText));
		} finally {
			TryDeleteDir(tempRoot);
		}
	}
}