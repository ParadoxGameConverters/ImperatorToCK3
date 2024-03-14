using commonItems;
using commonItems.Colors;
using commonItems.Mods;
using Fernandezja.ColorHashSharp;
using ImperatorToCK3.CK3.Cultures;
using ImperatorToCK3.UnitTests.TestHelpers;
using System;
using System.Collections.Generic;
using Xunit;

namespace ImperatorToCK3.UnitTests.CK3.Cultures; 

public class CultureCollectionTests {
	private static readonly ModFilesystem ck3ModFS = new("TestFiles/CK3/game", Array.Empty<Mod>());
	private static readonly PillarCollection pillars;
	private static readonly ColorFactory colorFactory = new();
	private static readonly List<string> ck3ModFlags = [];

	static CultureCollectionTests() {
		pillars = new PillarCollection(colorFactory, []) {
			new("test_heritage", new PillarData { Type = "heritage" }),
			new("test_language", new PillarData { Type = "language" })
		};
	}
	
	[Fact]
	public void ColorIsLoadedIfDefinedOrGeneratedIfMissing() {
		var cultures = new CultureCollection(colorFactory, pillars, ck3ModFlags);
		cultures.LoadNameLists(ck3ModFS);
		cultures.LoadCultures(ck3ModFS);

		var cultureWithColor = cultures["culture_with_color"];
		Assert.Equal(new Color(10, 20, 30), cultureWithColor.Color);
		
		var cultureWithoutDefinedColor = cultures["culture_without_color"];
		var colorHash = new ColorHash().Rgb(cultureWithoutDefinedColor.Id);
		var expectedColor = new Color(colorHash.R, colorHash.G, colorHash.B);
		Assert.Equal(expectedColor, cultureWithoutDefinedColor.Color);
	}

	[Fact]
	public void ConverterHeritageCanBeMergedIntoExistingHeritage() {
		// Existing heritage: "heritage_arberian" with culture "arberian"
		// Converter heritage: "heritage_arvanite" with cultures "albanian" (equivalent of "arberian") and "dalmatian"
		// Expected result: "heritage_arberian" with cultures "arberian" and "dalmatian"
		
		var cultures = new TestCK3CultureCollection();
		Assert.Empty(cultures);
		
		cultures.GenerateTestCulture("arberian", "heritage_arberian");
		Assert.Single(cultures);
		
		cultures.AddNameList(new NameList("name_list_albanian", new BufferedReader()));
		cultures.LoadConverterPillars("TestFiles/CK3/CultureCollectionTests/configurables/converter_pillars");
		cultures.LoadConverterCultures("TestFiles/CK3/CultureCollectionTests/configurables/converter_cultures.txt");
		
		Assert.Equal(2, cultures.Count);
		Assert.Equal("heritage_arberian", cultures["arberian"].Heritage.Id);
		Assert.Equal("heritage_arberian", cultures["dalmatian"].Heritage.Id);
	}

	[Fact]
	public void ConverterLanguageCanBeMergedIntoExistingLanguage() {
		// Existing language: "language_illyrian"
		// Converter language: "language_albanian" used by cultures "albanian" and "dalmatian"
		// Expected result: "language_illyrian" used by cultures "albanian" and "dalmatian"

		var cultures = new TestCK3CultureCollection();
		Assert.Empty(cultures);
		
		cultures.AddPillar(new("language_illyrian", new() {Type = "language"}));
		
		cultures.AddNameList(new NameList("name_list_albanian", new BufferedReader()));
		cultures.LoadConverterPillars("TestFiles/CK3/CultureCollectionTests/configurables/converter_pillars");
		cultures.LoadConverterCultures("TestFiles/CK3/CultureCollectionTests/configurables/converter_cultures.txt");
		
		Assert.Equal(2, cultures.Count);
		Assert.Equal("language_illyrian", cultures["albanian"].Language.Id);
		Assert.Equal("language_illyrian", cultures["dalmatian"].Language.Id);
	}
}