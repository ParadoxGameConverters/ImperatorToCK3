using commonItems.Mods;
using FluentAssertions;
using ImperatorToCK3.Imperator;
using ImperatorToCK3.Imperator.Religions;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace ImperatorToCK3.UnitTests.Imperator.Religions; 

public class ReligionCollectionTests {
	private const string ImperatorRoot = "TestFiles/Imperator/game";
	private static readonly List<Mod> mods = new() { new Mod("cool_mod", "TestFiles/documents/Imperator/mod/cool_mod")};
	private readonly ModFilesystem imperatorModFS = new (ImperatorRoot, mods);
	
	[Fact]
	public void ReligionsAreLoadedFromGameAndMods() {
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

	[Fact]
	public void DeitiesAreLoadedFromGameAndMods() {
		var scriptValues = new ScriptValueCollection();
		scriptValues.LoadScriptValues(imperatorModFS);
		
		var religions = new ReligionCollection(scriptValues);
		religions.LoadDeities(imperatorModFS);

		religions.Deities.Select(d => d.Id).Should().BeEquivalentTo(
			// deities from game
			"deity1",
			"deity2",
			"deity3",
			"deity4",
			"deity5",
			"deity6",
			"deity7",
			// deities from mod
			"mod_deity1",
			"mod_deity2"
		);
	}
}