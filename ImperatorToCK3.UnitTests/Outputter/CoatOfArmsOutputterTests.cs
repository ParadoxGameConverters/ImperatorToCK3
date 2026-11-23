using commonItems;
using commonItems.Colors;
using commonItems.Localization;
using commonItems.Mods;
using ImperatorToCK3.CK3.Characters;
using ImperatorToCK3.CK3.Cultures;
using ImperatorToCK3.CK3.Dynasties;
using ImperatorToCK3.CK3.Religions;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.CommonUtils.Map;
using ImperatorToCK3.Imperator.Countries;
using ImperatorToCK3.Imperator.Diplomacy;
using ImperatorToCK3.Imperator.Geography;
using ImperatorToCK3.Mappers.CoA;
using ImperatorToCK3.Mappers.Culture;
using ImperatorToCK3.Mappers.Government;
using ImperatorToCK3.Mappers.Nickname;
using ImperatorToCK3.Mappers.Province;
using ImperatorToCK3.Mappers.Region;
using ImperatorToCK3.Mappers.Religion;
using ImperatorToCK3.Mappers.SuccessionLaw;
using ImperatorToCK3.Mappers.TagTitle;
using ImperatorToCK3.Outputter;
using ImperatorToCK3.UnitTests.TestHelpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace ImperatorToCK3.UnitTests.Outputter;

public class CoatOfArmsOutputterTests {
	private const string ImperatorRoot = "TestFiles/CoatOfArmsOutputterTests/Imperator/game";
	private static readonly ModFilesystem irModFS = new(ImperatorRoot, Array.Empty<Mod>());
	private static readonly MapData irMapData = new(irModFS);
	private static readonly AreaCollection areas = new();
	private static readonly ImperatorRegionMapper irRegionMapper = new(areas, irMapData);
	
	public CoatOfArmsOutputterTests() {
		irRegionMapper.LoadRegions(irModFS, new ColorFactory());
	}

	[Fact]
	public async Task CoaIsOutputtedForCountryWithFlagSet() {
		var titles = new Title.LandedTitles();

		var countries = new CountryCollection();
		var countryReader = new BufferedReader("tag=ADI flag=testFlag");
		var country = Country.Parse(countryReader, 1);
		countries.Add(country);

		const string outputModPath = "output/outputMod";
		var outputPath = Path.Combine(outputModPath, "common/coat_of_arms/coat_of_arms/zzz_IRToCK3_coas.txt");
		SystemUtils.TryCreateFolder(CommonFunctions.GetPath(outputPath));

		var ck3Religions = new ReligionCollection(titles);
		var ck3RegionMapper = new CK3RegionMapper();
		var ck3ModFlags = new OrderedDictionary<string, bool>();
		titles.ImportImperatorCountries(countries,
			Array.Empty<Dependency>(),
			new TagTitleMapper(),
			new LocDB("english"),
			new TestCK3LocDB(),
			new ProvinceMapper(),
			new CoaMapper(irModFS),
			new GovernmentMapper(ck3GovernmentIds: Array.Empty<string>()),
			new SuccessionLawMapper(),
			new DefiniteFormMapper(),
			new ReligionMapper(ck3Religions, irRegionMapper, ck3RegionMapper),
			new CultureMapper(irRegionMapper, ck3RegionMapper, new CultureCollection(new ColorFactory(), new PillarCollection(new ColorFactory(), ck3ModFlags), ck3ModFlags)),
			new NicknameMapper(),
			new CharacterCollection(),
			new Date(400, 1, 1),
			new Configuration(),
			new List<KeyValuePair<Country, Dependency?>>(),
			enabledCK3Dlcs: []
		);

		await CoatOfArmsOutputter.OutputCoas(outputModPath, titles, new DynastyCollection(), new CoaMapper());

		var actualText = TextTestUtils.NormalizeNewlines(await File.ReadAllTextAsync(outputPath, TestContext.Current.CancellationToken));
		var expectedText = TextTestUtils.NormalizeNewlines(
			"""
			d_IRTOCK3_ADI={
				pattern="pattern_solid.tga"
				color1=red color2=green color3=blue
			}
			
			"""
		);

		Assert.Equal(expectedText, actualText);
	}

	[Fact]
	public async Task CoaIsNotOutputtedForCountryWithoutFlagSet() {
		var titles = new Title.LandedTitles();

		var countries = new CountryCollection();
		var countryReader = new BufferedReader("tag=BDI");
		var country = Country.Parse(countryReader, 2);
		countries.Add(country);

		const string outputModPath = "output/outputMod";
		var outputPath = Path.Combine(outputModPath, "common/coat_of_arms/coat_of_arms/zzz_IRToCK3_coas.txt");
		SystemUtils.TryCreateFolder(CommonFunctions.GetPath(outputPath));

		var ck3Religions = new ReligionCollection(titles);
		var ck3RegionMapper = new CK3RegionMapper();
		var ck3ModFlags = new OrderedDictionary<string, bool>();
		titles.ImportImperatorCountries(countries,
			Array.Empty<Dependency>(),
			new TagTitleMapper(),
			new LocDB("english"),
			new TestCK3LocDB(),
			new ProvinceMapper(),
			new CoaMapper(irModFS),
			new GovernmentMapper(ck3GovernmentIds: Array.Empty<string>()),
			new SuccessionLawMapper(),
			new DefiniteFormMapper(),
			new ReligionMapper(ck3Religions, irRegionMapper, ck3RegionMapper),
			new CultureMapper(irRegionMapper, ck3RegionMapper, new CultureCollection(new ColorFactory(), new PillarCollection(new ColorFactory(), ck3ModFlags), ck3ModFlags)),
			new NicknameMapper(),
			new CharacterCollection(),
			new Date(400, 1, 1),
			new Configuration(),
			new List<KeyValuePair<Country, Dependency?>>(),
			enabledCK3Dlcs: []
		);

		await CoatOfArmsOutputter.OutputCoas(outputModPath, titles, new DynastyCollection(), new CoaMapper());

		var actualText = await File.ReadAllTextAsync(outputPath, TestContext.Current.CancellationToken);
		Assert.True(string.IsNullOrWhiteSpace(actualText));
	}

	[Fact]
	public async Task CoasMatchingVanillaMapperAreSkipped_VariablesAndDynastyCoasAreOutputted() {
		var tempDir = CreateTempDir();
		try {
			var titles = new Title.LandedTitles();

			// The mapper used for setting title CoAs and the one passed to the outputter differ only for k_changed.
			const string sameCoaDefinition = @"k_same = { pattern=""pattern_solid.tga"" color1=red color2=green color3=blue }";
			var vanillaCoasDir = Path.Combine(tempDir, "vanilla", "common", "coat_of_arms", "coat_of_arms");
			Directory.CreateDirectory(vanillaCoasDir);
			await File.WriteAllTextAsync(Path.Combine(vanillaCoasDir, "coas.txt"),
				string.Join('\n',
					"@smCross = 0.22",
					sameCoaDefinition,
					@"k_changed = { pattern=""pattern_solid.tga"" color1=red color2=green color3=blue }"
				), TestContext.Current.CancellationToken);

			var expectedCoasDir = Path.Combine(tempDir, "expected", "common", "coat_of_arms", "coat_of_arms");
			Directory.CreateDirectory(expectedCoasDir);
			await File.WriteAllTextAsync(Path.Combine(expectedCoasDir, "coas.txt"),
				string.Join('\n',
					"@smCross = 0.22",
					sameCoaDefinition,
					@"k_changed = { pattern=""pattern_argent.tga"" color1=red color2=green color3=blue }"
				), TestContext.Current.CancellationToken);

			var settingMapper = new CoaMapper(new ModFilesystem(Path.Combine(tempDir, "vanilla"), Array.Empty<Mod>()));
			var outputMapper = new CoaMapper(new ModFilesystem(Path.Combine(tempDir, "expected"), Array.Empty<Mod>()));

			titles.Add("k_same");
			titles.Add("k_changed");
			titles.SetCoatsOfArms(settingMapper);

			var dynasties = new DynastyCollection();
			var dynastyWithCoa = new Dynasty("dynn_coatest", new BufferedReader("name = Coatest")) {
				CoA = new StringOfItem("@dynasty_coa_gfx")
			};
			var dynastyWithoutCoa = new Dynasty("dynn_plain", new BufferedReader("name = Plain"));
			dynasties.Add(dynastyWithCoa);
			dynasties.Add(dynastyWithoutCoa);

			const string outputModPath = "output/outputMod";
			var outputPath = Path.Combine(outputModPath, "common/coat_of_arms/coat_of_arms/zzz_IRToCK3_coas.txt");
			SystemUtils.TryCreateFolder(CommonFunctions.GetPath(outputPath));

			await CoatOfArmsOutputter.OutputCoas(outputModPath, titles, dynasties, outputMapper);

			var actualText = TextTestUtils.NormalizeNewlines(await File.ReadAllTextAsync(outputPath, TestContext.Current.CancellationToken));
			// k_same's CoA equals the one in the passed mapper, so it is skipped.
			Assert.DoesNotContain("k_same=", actualText, StringComparison.Ordinal);
			Assert.Contains("k_changed=", actualText, StringComparison.Ordinal);
			Assert.Contains("@smCross=0.22", actualText, StringComparison.Ordinal);
			Assert.Contains("dynn_coatest=", actualText, StringComparison.Ordinal);
			Assert.DoesNotContain("dynn_plain=", actualText, StringComparison.Ordinal);
		} finally {
			TryDeleteDir(tempDir);
		}
	}

	[Fact]
	public void CopyCoaPatternsCopiesPatternFilesRecursively() {
		var tempDir = CreateTempDir();
		try {
			var irRoot = Path.Combine(tempDir, "ir_root");
			var patternsSource = Path.Combine(irRoot, "gfx", "coat_of_arms", "patterns", "subfolder");
			Directory.CreateDirectory(patternsSource);
			var sourceFilePath = Path.Combine(patternsSource, "..", "pattern_a.dds");
			File.WriteAllText(sourceFilePath, "patternA");
			File.WriteAllText(Path.Combine(patternsSource, "pattern_b.dds"), "patternB");

			const string outputModPath = "output/outputMod";
			var destPatternsRoot = Path.Combine(outputModPath, "gfx", "coat_of_arms", "patterns");
			SystemUtils.TryCreateFolder(destPatternsRoot);
			// File.Copy does not create destination directories.
			SystemUtils.TryCreateFolder(Path.Combine(destPatternsRoot, "subfolder"));

			var sourceModFS = new ModFilesystem(irRoot, Array.Empty<Mod>());
			CoatOfArmsOutputter.CopyCoaPatterns(sourceModFS, outputModPath);

			Assert.True(File.Exists(Path.Combine(destPatternsRoot, "pattern_a.dds")));
			Assert.True(File.Exists(Path.Combine(destPatternsRoot, "subfolder", "pattern_b.dds")));
		} finally {
			TryDeleteDir(tempDir);
		}
	}

	private static string CreateTempDir() {
		var dir = Path.Combine(Path.GetTempPath(), "ImperatorToCK3_UnitTests", "CoatOfArmsOutputter", Guid.NewGuid().ToString("N"));
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
}