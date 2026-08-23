using commonItems.Mods;
using ImperatorToCK3.Imperator.Geography;
using ImperatorToCK3.Imperator.Provinces;
using System;
using System.IO;
using Xunit;

namespace ImperatorToCK3.UnitTests.Imperator.Geography;

[Collection("Sequential")]
[CollectionDefinition("Sequential", DisableParallelization = true)]
public class AreaCollectionTests {
	[Fact]
	public void IgnoredAreaKeywordsAreLogged() {
		var output = new StringWriter();
		Console.SetOut(output);

		Area.IgnoredKeywords.Clear();
		var imperatorRoot = Path.Combine("TestFiles", "AreaCollectionTests");
		var modFS = new ModFilesystem(imperatorRoot, Array.Empty<Mod>());

		var areas = new AreaCollection();
		areas.LoadAreas(modFS, []);

		Assert.Single(areas);
		Assert.NotNull(areas["test_area"]);
		Assert.Contains("[DEBUG] Ignored area keywords: color", output.ToString());
	}

	[Fact]
	public void NoIgnoredKeywordsAreLoggedWhenAllAreaKeywordsAreKnown() {
		var output = new StringWriter();
		Console.SetOut(output);

		Area.IgnoredKeywords.Clear();
		var imperatorRoot = Path.Combine("TestFiles", "StateTests");
		var modFS = new ModFilesystem(imperatorRoot, Array.Empty<Mod>());

		var areas = new AreaCollection();
		areas.LoadAreas(modFS, []);

		Assert.Single(areas);
		Assert.DoesNotContain("Ignored area keywords", output.ToString());
	}
}
