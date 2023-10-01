﻿using commonItems;
using commonItems.Colors;
using commonItems.Mods;
using Fernandezja.ColorHashSharp;
using ImperatorToCK3.CK3.Cultures;
using System;
using Xunit;

namespace ImperatorToCK3.UnitTests.CK3.Cultures; 

public class CultureCollectionTests {
	private static readonly ModFilesystem CK3ModFS = new("TestFiles/CK3/game", Array.Empty<Mod>());
	private static readonly PillarCollection Pillars;
	private static readonly ColorFactory ColorFactory = new();

	static CultureCollectionTests() {
		Pillars = new PillarCollection { new("test_heritage", new BufferedReader("type = heritage")) };
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
}