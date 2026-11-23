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
	private static readonly OrderedDictionary<string, bool> ck3ModFlags = new() {
		{"tfe", false}, {"wtwsms", false}, {"roa", false}, {"vanilla_ck3", true}
	};

	static CultureCollectionTests() {
		pillars = new PillarCollection(colorFactory, ck3ModFlags) {
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
		
		var cultures = new TestCK3CultureCollection(ck3ModFlags);
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

		var cultures = new TestCK3CultureCollection(ck3ModFlags);
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

	[Fact]
	public void WarnAboutCircularParentsLogsWarningWhenParentCultureIsNotFound() {		var cultures = new TestCK3CultureCollection();
		// "french" has "roman" as its parent, but "roman" is not in the collection.
		cultures.GenerateTestCulture("french", "heritage_latin");
		cultures["french"].ParentCultureIds.Add("roman");

		var output = new StringWriter();
		Console.SetOut(output);
		cultures.WarnAboutCircularParents();
		var outputString = output.ToString();

		Assert.Contains("[WARN] Parent culture roman not found for culture french!", outputString);

		// The missing parent doesn't make the culture its own ancestor, so no error is logged.
		Assert.DoesNotContain("[ERROR]", outputString);
	}

	[Fact]
	public void CulturesWithMissingHeritageLanguageOrNameListAreSkipped() {
		var tempFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".txt");
		try {
			File.WriteAllText(tempFile, """
				good_culture = {
					color = rgb { 10 20 30 }
					heritage = test_heritage
					language = test_language
					name_list = test_name_list
				}
				culture_without_heritage = {
					heritage = missing_heritage
					language = test_language
					name_list = test_name_list
				}
				culture_without_language = {
					heritage = test_heritage
					language = missing_language
					name_list = test_name_list
				}
				culture_without_namelist = {
					heritage = test_heritage
					language = test_language
					name_list = missing_name_list
				}
				""");

			var cultures = new TestCK3CultureCollection();
			cultures.AddPillar(new("test_heritage", new() { Type = "heritage" }));
			cultures.AddPillar(new("test_language", new() { Type = "language" }));
			cultures.AddNameList(new ImperatorToCK3.CK3.Cultures.NameList("test_name_list", new BufferedReader()));

			var output = new StringWriter();
			Console.SetOut(output);
			cultures.LoadConverterCultures(tempFile);
			var log = output.ToString();

			Assert.True(cultures.ContainsKey("good_culture"));
			Assert.False(cultures.ContainsKey("culture_without_heritage"));
			Assert.False(cultures.ContainsKey("culture_without_language"));
			Assert.False(cultures.ContainsKey("culture_without_namelist"));
			Assert.Contains("has no valid heritage defined! Skipping.", log);
			Assert.Contains("has no valid language defined! Skipping.", log);
			Assert.Contains("has no name list defined! Skipping.", log);
		} finally {
			File.Delete(tempFile);
		}
	}

	[Fact]
	public void InvalidatedCulturesAreSkippedAndOptionalOnesAreLoaded() {
		var tempFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".txt");
		try {
			File.WriteAllText(tempFile, """
				existing_culture = {
					heritage = test_heritage
					language = test_language
					name_list = test_name_list
				}
				invalidated_culture = {
					INVALIDATED_BY = { vanilla_ck3 = { existing_culture } }
					heritage = test_heritage
					language = test_language
					name_list = test_name_list
				}
				optional_culture = {
					INVALIDATED_BY = { vanilla_ck3 = { nonexistent_culture } }
					heritage = test_heritage
					language = test_language
					name_list = test_name_list
				}
				""");

			var cultures = new TestCK3CultureCollection();
			cultures.AddPillar(new("test_heritage", new() { Type = "heritage" }));
			cultures.AddPillar(new("test_language", new() { Type = "language" }));
			cultures.AddNameList(new ImperatorToCK3.CK3.Cultures.NameList("test_name_list", new BufferedReader()));
			cultures.LoadConverterCultures(tempFile);

			Assert.True(cultures.ContainsKey("existing_culture"));
			Assert.False(cultures.ContainsKey("invalidated_culture"));
			Assert.True(cultures.ContainsKey("optional_culture"));
		} finally {
			File.Delete(tempFile);
		}
	}

	[Fact]
	public void LoadInnovationIds_LoadsIdsFromModFilesystem() {
		var tempRoot = Path.Combine(Path.GetTempPath(), "CultureInnovationIdsTests", Guid.NewGuid().ToString("N"));
		try {
			Directory.CreateDirectory(Path.Combine(tempRoot, "common", "culture", "innovations"));
			File.WriteAllText(
				Path.Combine(tempRoot, "common", "culture", "innovations", "innovations.txt"),
				"innovation_test_1 = {}\ninnovation_test_2 = {}\n");
			var modFS = new ModFilesystem(tempRoot, Array.Empty<Mod>());

			var cultures = new TestCK3CultureCollection();
			cultures.LoadInnovationIds(modFS);

			var idsField = typeof(CultureCollection).GetField("InnovationIds",
				System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
			var ids = (System.Collections.Generic.HashSet<string>)idsField.GetValue(cultures)!;
			Assert.Contains("innovation_test_1", ids);
			Assert.Contains("innovation_test_2", ids);
		} finally {
			Directory.Delete(tempRoot, recursive: true);
		}
	}
}