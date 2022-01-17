using commonItems;
using commonItems.Localization;
using ImperatorToCK3.CK3.Dynasties;
using ImperatorToCK3.Imperator.Families;
using ImperatorToCK3.Outputter;
using System.IO;
using Xunit;

namespace ImperatorToCK3.UnitTests.Outputter;

public class DynastiesOutputterTests {
	[Fact]
	public void DynastiesAreOutputted() {
		const string outputModName = "outputMod";
		var locDB = new LocDB("english");

		var dynasties = new DynastyCollection();
		var family1 = new Family(1);
		var dynasty1 = new Dynasty(family1, locDB);
		dynasties.Add(dynasty1);
		var family2 = new Family(2);
		var dynasty2 = new Dynasty(family2, locDB) {
			Culture = "roman"
		};
		dynasties.Add(dynasty2);

		var outputPath = Path.Combine("output", outputModName, "common", "dynasties", "imp_dynasties.txt");
		SystemUtils.TryCreateFolder(CommonFunctions.GetPath(outputPath));
		DynastiesOutputter.OutputDynasties(outputModName, dynasties);

		using var file = File.OpenRead(outputPath);
		var reader = new StreamReader(file);

		Assert.Equal("dynn_IMPTOCK3_1={", reader.ReadLine());
		Assert.Equal("\tname=\"dynn_IMPTOCK3_1\"", reader.ReadLine());
		Assert.Equal("}", reader.ReadLine());

		Assert.Equal("dynn_IMPTOCK3_2={", reader.ReadLine());
		Assert.Equal("\tname=\"dynn_IMPTOCK3_2\"", reader.ReadLine());
		Assert.Equal("\tculture=\"roman\"", reader.ReadLine());
		Assert.Equal("}", reader.ReadLine());
		Assert.True(reader.EndOfStream);
	}
}