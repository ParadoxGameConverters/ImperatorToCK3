using commonItems;
using commonItems.Colors;
using commonItems.Mods;
using Fernandezja.ColorHashSharp;
using ImperatorToCK3.CK3.Cultures;
using ImperatorToCK3.UnitTests.TestHelpers;
using System;
using System.Linq;
using Xunit;

namespace ImperatorToCK3.UnitTests.CK3.Cultures; 

public class CultureCollectionTests {
	private static readonly ModFilesystem CK3ModFS = new("TestFiles/CK3/game", Array.Empty<Mod>());
	private static readonly PillarCollection Pillars;
	private static readonly ColorFactory ColorFactory = new();

	static CultureCollectionTests() {
		Pillars = new PillarCollection(ColorFactory) { new("test_heritage", new PillarData { Type = "heritage" }) };
	}
	
	[Fact]
	public void ColorIsLoadedIfDefinedOrGeneratedIfMissing() {
		var cultures = new CultureCollection(ColorFactory, Pillars);
		cultures.LoadNameLists(CK3ModFS);
		cultures.LoadCultures(CK3ModFS);

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
		// Converter heritage: "heritage_arvanite" with cultures "albanian" and "dalmatian"
		// Expected result: "heritage_arberian" with cultures "arberian" and "dalmatian"
		
		var cultures = new TestCK3CultureCollection();
		cultures.GenerateTestCulture("arberian", "heritage_arberian");
		cultures.LoadConverterPillars("TestFiles/configurables/converter_pillars.txt"); // TODO: FIX PATH
		cultures.LoadConverterCultures("TestFiles/configurables/converter_cultures.txt"); // TODO: FIX PATH
		
		Assert.Equal(2, cultures.Count);
		Assert.Equal("heritage_arberian", cultures["arberian"].Heritage.Id);
		Assert.Equal("heritage_arberian", cultures["dalmatian"].Heritage.Id);
	}
}