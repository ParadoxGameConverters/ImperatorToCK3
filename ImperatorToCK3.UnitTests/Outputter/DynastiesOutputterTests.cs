﻿using commonItems;
using commonItems.Colors;
using commonItems.Localization;
using commonItems.Mods;
using ImperatorToCK3.CK3.Cultures;
using ImperatorToCK3.CK3.Dynasties;
using ImperatorToCK3.Imperator.Characters;
using ImperatorToCK3.Imperator.Cultures;
using ImperatorToCK3.Imperator.Families;
using ImperatorToCK3.Imperator.Geography;
using ImperatorToCK3.Mappers.Culture;
using ImperatorToCK3.Mappers.Region;
using ImperatorToCK3.Outputter;
using System;
using System.IO;
using Xunit;

namespace ImperatorToCK3.UnitTests.Outputter;

public class DynastiesOutputterTests {
	private static readonly Date ConversionDate = new(867, 1, 1);
	
	[Fact]
	public void DynastiesAreOutputted() {
		const string outputModName = "outputMod";
		var locDB = new LocDB("english");
		const string imperatorRoot = "TestFiles/Imperator/root";
		ModFilesystem irModFS = new(imperatorRoot, Array.Empty<Mod>());
		AreaCollection areas = new();
		ImperatorRegionMapper irRegionMapper = new(areas);
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

		var outputPath = Path.Combine("output", outputModName, "common/dynasties/ir_dynasties.txt");
		if (File.Exists(outputPath)) {
			// clean up from previous runs.
			File.Delete(outputPath);
		}
		SystemUtils.TryCreateFolder(CommonFunctions.GetPath(outputPath));
		DynastiesOutputter.OutputDynasties(outputModName, dynasties);

		using var file = File.OpenRead(outputPath);
		var reader = new StreamReader(file);

		Assert.Equal("dynn_irtock3_1={", reader.ReadLine());
		Assert.Equal("\tname = dynn_irtock3_1", reader.ReadLine());
		Assert.Equal("}", reader.ReadLine());

		Assert.Equal("dynn_irtock3_2={", reader.ReadLine());
		Assert.Equal("\tname = dynn_irtock3_2", reader.ReadLine());
		Assert.Equal("\tculture = roman", reader.ReadLine());
		Assert.Equal("}", reader.ReadLine());
		Assert.True(string.IsNullOrWhiteSpace(reader.ReadLine()));
		Assert.True(reader.EndOfStream);
	}
}