using commonItems;
using commonItems.Colors;
using commonItems.Localization;
using commonItems.Mods;
using ImperatorToCK3.CK3.Cultures;
using ImperatorToCK3.CK3.Dynasties;
using ImperatorToCK3.CommonUtils.Map;
using ImperatorToCK3.Imperator.Characters;
using ImperatorToCK3.Imperator.Cultures;
using ImperatorToCK3.Imperator.Families;
using ImperatorToCK3.Imperator.Geography;
using ImperatorToCK3.Mappers.Culture;
using ImperatorToCK3.Mappers.Region;
using ImperatorToCK3.Outputter;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace ImperatorToCK3.UnitTests.Outputter;

public class DynastiesOutputterTests {
	private static readonly Date ConversionDate = new(867, 1, 1);
	
	[Fact]
	public async Task DynastiesAreOutputted() {
		const string outputModPath = "output/outputMod";
		var locDB = new LocDB("english");
		const string imperatorRoot = "TestFiles/Imperator/root";
		ModFilesystem irModFS = new(imperatorRoot, Array.Empty<Mod>());
		var irMapData = new MapData(irModFS);
		AreaCollection areas = new();
		ImperatorRegionMapper irRegionMapper = new(areas, irMapData);
		irRegionMapper.LoadRegions(irModFS, new ColorFactory());
		var colorFactory = new ColorFactory();
		irRegionMapper.LoadRegions(irModFS, colorFactory);
		var ck3ModFlags = Array.Empty<string>();
		CultureMapper cultureMapper = new(irRegionMapper, new CK3RegionMapper(), new CultureCollection(colorFactory, new PillarCollection(colorFactory, ck3ModFlags), ck3ModFlags));

		var characters = new CharacterCollection();
		var dynasties = new DynastyCollection();
		var cultures = new CulturesDB();
		
		var family1 = new Family(1);
		var dynasty1 = new Dynasty(family1, characters, cultures, cultureMapper, locDB, ConversionDate);
		dynasties.Add(dynasty1);
		
		var family2 = new Family(2);
		var dynasty2 = new Dynasty(family2, characters, cultures, cultureMapper, locDB, ConversionDate) {
			CultureId = "roman"
		};
		dynasties.Add(dynasty2);

		var outputPath = Path.Combine(outputModPath, "common/dynasties/irtock3_all_dynasties.txt");
		if (File.Exists(outputPath)) {
			// clean up from previous runs.
			File.Delete(outputPath);
		}
		SystemUtils.TryCreateFolder(CommonFunctions.GetPath(outputPath));
		await DynastiesOutputter.OutputDynasties(outputModPath, dynasties);

		await using var file = File.OpenRead(outputPath);
		var reader = new StreamReader(file);

		Assert.Equal("dynn_irtock3_1={", await reader.ReadLineAsync());
		Assert.Equal("\tname = dynn_irtock3_1", await reader.ReadLineAsync());
		Assert.Equal("}", await reader.ReadLineAsync());

		Assert.Equal("dynn_irtock3_2={", await reader.ReadLineAsync());
		Assert.Equal("\tname = dynn_irtock3_2", await reader.ReadLineAsync());
		Assert.Equal("\tculture = roman", await reader.ReadLineAsync());
		Assert.Equal("}", await reader.ReadLineAsync());
		Assert.True(string.IsNullOrWhiteSpace(await reader.ReadLineAsync()));
		Assert.True(reader.EndOfStream);
	}
}