using commonItems;
using commonItems.Colors;
using commonItems.Mods;
using Fernandezja.ColorHashSharp;
using ImperatorToCK3.CK3.Cultures;
using ImperatorToCK3.UnitTests.TestHelpers;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace ImperatorToCK3.UnitTests.CK3.Cultures; 

[Collection("Sequential")]
public class CultureCollectionTests {
	private static readonly ModFilesystem ck3ModFS = new("TestFiles/CK3/game", Array.Empty<Mod>());
	private static readonly PillarCollection pillars;
	private static readonly ColorFactory colorFactory = new();
	private static readonly OrderedDictionary<string, bool> ck3ModFlags = new() {{"tfe", false}, {"wtwsms", false}, {"roa", false}};

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

	[Fact]
	public void WarnAboutCircularParentsLogsCorrectWarningsForAPairOfCultures() {
		var cultures = new TestCK3CultureCollection();
		
		// Create a circular dependency by making "french" a child of "roman"
		// and "roman" a child of "french".
		cultures.GenerateTestCulture("roman", "heritage_latin");
		cultures.GenerateTestCulture("french", "heritage_latin");
		cultures["french"].ParentCultureIds.Add("roman");
		cultures["roman"].ParentCultureIds.Add("french");
		
		var output = new StringWriter();
		Console.SetOut(output);
		cultures.WarnAboutCircularParents();
		var outputString = output.ToString();
		
		Assert.Contains("[ERROR] Culture french is set as its own direct or indirect parent!", outputString);
		Assert.Contains("[ERROR] Culture roman is set as its own direct or indirect parent!", outputString);
	}

	[Fact]
	public void WarnAboutCircularParentsLogsCorrectWarningForCultureBeingItsOwnParent() {
		var cultures = new TestCK3CultureCollection();
		// Create a culture that is its own parent.
		cultures.GenerateTestCulture("roman", "heritage_latin");
		cultures["roman"].ParentCultureIds.Add("roman");
		
		var output = new StringWriter();
		Console.SetOut(output);
		cultures.WarnAboutCircularParents();
		var outputString = output.ToString();
		
		Assert.Contains("[ERROR] Culture roman is set as its own direct or indirect parent!", outputString);
	}
	
	[Fact]
	public void WarnAboutCircularParentsDoesNotLogAnythingForValidCultures() {
		var cultures = new TestCK3CultureCollection();
		// Just French being a child of Roman, no circular dependency.
		cultures.GenerateTestCulture("roman", "heritage_latin");
		cultures.GenerateTestCulture("french", "heritage_latin");
		cultures["french"].ParentCultureIds.Add("roman");
		
		var output = new StringWriter();
		Console.SetOut(output);
		cultures.WarnAboutCircularParents();
		var outputString = output.ToString();
		
		Assert.DoesNotContain("[ERROR]", outputString);
	}
}