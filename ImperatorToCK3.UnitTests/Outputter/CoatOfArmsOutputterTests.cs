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
		var ck3ModFlags = new List<string>();
		titles.ImportImperatorCountries(countries,
			Array.Empty<Dependency>(),
			new TagTitleMapper(),
			new LocDB("english"),
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
			new List<KeyValuePair<Country, Dependency?>>()
		);

		await CoatOfArmsOutputter.OutputCoas(outputModPath, titles, new List<Dynasty>());

		await using var file = File.OpenRead(outputPath);
		var reader = new StreamReader(file);

		Assert.Equal("d_IRTOCK3_ADI={", await reader.ReadLineAsync());
		Assert.Equal("\tpattern=\"pattern_solid.tga\"", await reader.ReadLineAsync());
		Assert.Equal("\tcolor1=red color2=green color3=blue", await reader.ReadLineAsync());
		Assert.Equal("}", await reader.ReadLineAsync());
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
		var ck3ModFlags = new List<string>();
		titles.ImportImperatorCountries(countries,
			Array.Empty<Dependency>(),
			new TagTitleMapper(),
			new LocDB("english"),
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
			new List<KeyValuePair<Country, Dependency?>>()
		);

		await CoatOfArmsOutputter.OutputCoas(outputModPath, titles, new List<Dynasty>());

		await using var file = File.OpenRead(outputPath);
		var reader = new StreamReader(file);

		Assert.True(reader.EndOfStream);
	}
}