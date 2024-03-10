using commonItems.Mods;
using ImperatorToCK3.CK3.Cultures;
using System;
using System.IO;
using Xunit;

namespace ImperatorToCK3.UnitTests.CK3.Cultures;

[Collection("Sequential")]
[CollectionDefinition("Sequential", DisableParallelization = true)]
public class PillarCollectionTests {
	[Fact]
	public void WarningIsLoggedWhenPillarDataIsMissingType() {
		Directory.CreateDirectory("pillars_test");
		Directory.CreateDirectory("pillars_test/common");
		Directory.CreateDirectory("pillars_test/common/culture");
		Directory.CreateDirectory("pillars_test/common/culture/pillars");
		var pillarsFile = File.CreateText("pillars_test/common/culture/pillars/test_pillars.txt");
		pillarsFile.WriteLine("pillar_without_type = {}");
		pillarsFile.Close();
		
		var modFS = new ModFilesystem("pillars_test", Array.Empty<Mod>());
		var collection = new PillarCollection(new commonItems.Colors.ColorFactory(), []);
		
		var consoleOut = new StringWriter();
		Console.SetOut(consoleOut);
		collection.LoadPillars(modFS);
		Assert.Contains("[WARN] Pillar pillar_without_type has no type defined! Skipping.", consoleOut.ToString());
	}
}