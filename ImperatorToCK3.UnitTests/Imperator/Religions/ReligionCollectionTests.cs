using commonItems.Mods;
using FluentAssertions;
using ImperatorToCK3.Imperator;
using ImperatorToCK3.Imperator.Religions;
using System.Linq;
using Xunit;

namespace ImperatorToCK3.UnitTests.Imperator.Religions; 

public class ReligionCollectionTests {
	private const string ImperatorRoot = "TestFiles/Imperator/game";
	
	[Fact]
	public void ReligionsAreLoadedFromGameAndMods() {
		var mods = new[] { new Mod("cool_mod", "TestFiles/documents/Imperator/mod/cool_mod")};
		var imperatorModFS = new ModFilesystem(ImperatorRoot, mods);
		var scriptValues = new ScriptValueCollection();
		scriptValues.LoadScriptValues(imperatorModFS);

		var religions = new ReligionCollection(scriptValues);
		religions.LoadReligions(imperatorModFS);
		
		religions.Select(r => r.Id).Should().BeEquivalentTo(
			"roman_pantheon", // from game
			"judaism", // from game
			"egyptian_pantheon", // from mod
			"carthaginian_pantheon" // from mod
		);
	}
}