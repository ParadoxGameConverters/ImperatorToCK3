using commonItems;
using commonItems.Colors;
using commonItems.Localization;
using commonItems.Mods;
using ImperatorToCK3.CK3.Characters;
using ImperatorToCK3.CK3.Cultures;
using ImperatorToCK3.CK3.Provinces;
using ImperatorToCK3.CK3.Religions;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.CK3.Wars;
using ImperatorToCK3.CommonUtils.Map;
using ImperatorToCK3.Imperator.Countries;
using ImperatorToCK3.Imperator.Diplomacy;
using ImperatorToCK3.Imperator.Geography;
using ImperatorToCK3.Imperator.States;
using ImperatorToCK3.Mappers.CoA;
using ImperatorToCK3.Mappers.Culture;
using ImperatorToCK3.Mappers.Government;
using ImperatorToCK3.Mappers.Nickname;
using ImperatorToCK3.Mappers.Province;
using ImperatorToCK3.Mappers.Region;
using ImperatorToCK3.Mappers.Religion;
using ImperatorToCK3.Mappers.SuccessionLaw;
using ImperatorToCK3.Mappers.TagTitle;
using ImperatorToCK3.Mappers.War;
using ImperatorToCK3.Outputter;
using ImperatorToCK3.UnitTests.TestHelpers;
using War = ImperatorToCK3.CK3.Wars.War;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace ImperatorToCK3.UnitTests.Outputter;

// WarMapping.Parse and Imperator's War.Parse use static mutable parser state,
// so this class must not run in parallel with other test collections.
[CollectionDefinition(nameof(WarsOutputterTests), DisableParallelization = true)]
public sealed class WarsOutputterTestsDefinition;

[Collection(nameof(WarsOutputterTests))]
public class WarsOutputterTests {
	private static readonly Date ConversionDate = new(867, 1, 1);

	private static string CreateTempDir() {
		var dir = Path.Combine(Path.GetTempPath(), "ImperatorToCK3_UnitTests", "WarsOutputter", Guid.NewGuid().ToString("N"));
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

	private static (Title.LandedTitles titles, CountryCollection countries) ImportTestCountries(CharacterCollection characters) {
		var imperatorRoot = "TestFiles/CoatOfArmsOutputterTests/Imperator/game";
		var irModFS = new ModFilesystem(imperatorRoot, Array.Empty<Mod>());
		var irMapData = new MapData(irModFS);
		var areas = new AreaCollection();
		var irRegionMapper = new ImperatorRegionMapper(areas, irMapData);
		irRegionMapper.LoadRegions(irModFS, new ColorFactory());

		var titles = new Title.LandedTitles();
		var countries = new CountryCollection();
		countries.Add(Country.Parse(new BufferedReader("tag=ADI flag=flagADI"), 1));
		countries.Add(Country.Parse(new BufferedReader("tag=BRU flag=flagBRU"), 2));

		var ck3Religions = new ReligionCollection(titles);
		var ck3RegionMapper = new CK3RegionMapper();
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
			new CultureMapper(irRegionMapper, ck3RegionMapper, new CultureCollection(new ColorFactory(), new PillarCollection(new ColorFactory(), []), [])),
			new NicknameMapper(),
			characters,
			new Date(400, 1, 1),
			new Configuration(),
			new List<KeyValuePair<Country, Dependency?>>(),
			enabledCK3Dlcs: []
		);

		return (titles, countries);
	}

	private static async Task<WarMapper> CreateWarMapperAsync(string tempDir) {
		var mapperPath = Path.Combine(tempDir, "war_mappings.txt");
		await File.WriteAllTextAsync(mapperPath,
			"""
			link = {
				ir = take_province
				ck3 = cb_invasion
			}
			""", TestContext.Current.CancellationToken);
		return new WarMapper(mapperPath);
	}

	private static War CreateWar(string warText, Title.LandedTitles titles, CountryCollection countries, WarMapper warMapper) {
		var irWar = ImperatorToCK3.Imperator.Diplomacy.War.Parse(new BufferedReader(warText));
		return new War(
			irWar,
			warMapper,
			new ProvinceMapper(),
			countries,
			new StateCollection(),
			new ProvinceCollection(),
			titles,
			ConversionDate
		);
	}

	[Fact]
	public async Task OutputWarsWritesWarsWithAndWithoutCasusBelli() {
		var tempDir = CreateTempDir();
		try {
			Directory.CreateDirectory(Path.Combine(tempDir, "history", "wars"));

			var characters = new CharacterCollection();
			var (titles, countries) = ImportTestCountries(characters);

			var attackerA = new Character("char_a", "A", new Date(800, 1, 1), characters);
			var defenderB = new Character("char_b", "B", new Date(800, 1, 1), characters);
			characters.AddOrReplace(attackerA);
			characters.AddOrReplace(defenderB);
			titles["d_IRTOCK3_ADI"].SetHolder(attackerA, ConversionDate);
			titles["d_IRTOCK3_BRU"].SetHolder(defenderB, ConversionDate);

			var warMapper = await CreateWarMapperAsync(tempDir);

			// Mapped wargoal -> casus belli is set. Start date year is above 2 so it is not clamped.
			var warWithCB = CreateWar("start_date=800.1.1 attacker=1 defender=2 take_province={type=take_province}", titles, countries, warMapper);
			// Unmapped wargoal -> casus belli stays null. AUC date below year 2 AD gets clamped to 2.1.1.
			var warWithoutCB = CreateWar("start_date=1.1.1 attacker=2 defender=1 superiority={type=superiority}", titles, countries, warMapper);

			Assert.Equal("cb_invasion", warWithCB.CasusBelli);
			Assert.Null(warWithoutCB.CasusBelli);
			Assert.Equal("char_a", warWithCB.Attackers[0]);
			Assert.Equal("char_b", warWithCB.Defenders[0]);
			Assert.True(warWithCB.TargetedTitles.Contains("d_IRTOCK3_BRU")); // capital county fallback
			Assert.Equal(new Date(2, 1, 1), warWithoutCB.StartDate);

			await WarsOutputter.OutputWars(tempDir, [warWithCB, warWithoutCB]);

			var output = TextTestUtils.NormalizeNewlines(await File.ReadAllTextAsync(
				Path.Combine(tempDir, "history", "wars", "00_wars.txt"),
				TestContext.Current.CancellationToken));
			Assert.Contains("casus_belli = cb_invasion", output, StringComparison.Ordinal);
			Assert.Contains("start_date = 2.1.1", output, StringComparison.Ordinal);
			Assert.Contains("targeted_titles={ d_IRTOCK3_BRU }", output, StringComparison.Ordinal);
			Assert.DoesNotContain("casus_belli =", output.Replace("casus_belli = cb_invasion", string.Empty), StringComparison.Ordinal);
		} finally {
			TryDeleteDir(tempDir);
		}
	}
}
