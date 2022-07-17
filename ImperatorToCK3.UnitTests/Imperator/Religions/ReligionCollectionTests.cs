using commonItems;
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

		// test if modifiers are correctly loaded
		Assert.Collection(religions["judaism"].Modifiers,
			kvp => {
				Assert.Equal("global_pop_assimilation_speed_modifier", kvp.Key);
				Assert.Equal(0.2, kvp.Value);
			},
			kvp => {
				Assert.Equal("omen_duration", kvp.Key);
				Assert.Equal(0.5, kvp.Value);
			},
			kvp => {
				Assert.Equal("omen_power", kvp.Key);
				Assert.Equal(0.15, kvp.Value);
			});
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

	[Fact]
	public void GetDeityForHolySiteIdReturnsCorrectDeityOrNullWhenIdIsNotFoundOrWhenDeityIsNotFound() {
		var scriptValues = new ScriptValueCollection();
		scriptValues.LoadScriptValues(imperatorModFS);
		
		var religions = new ReligionCollection(scriptValues);
		religions.LoadDeities(imperatorModFS);
		var deityManagerReader = new BufferedReader(
			@"deities_database = {
				1 = { deity=""deity1"" }
				2 = { deity=""deity2"" }
				3 = { deity=""deity3"" }
				404 = { deity=""undefined_deity"" }
			}"
		);
		religions.LoadHolySiteDatabase(deityManagerReader);
		
		var deity = religions.GetDeityForHolySiteId(1);
		Assert.NotNull(deity);
		Assert.Equal("deity1", deity.Id);
		deity = religions.GetDeityForHolySiteId(2);
		Assert.NotNull(deity);
		Assert.Equal("deity2", deity.Id);
		deity = religions.GetDeityForHolySiteId(3);
		Assert.NotNull(deity);
		Assert.Equal("deity3", deity.Id);

		// holy site ID not found
		deity = religions.GetDeityForHolySiteId(4);
		Assert.Null(deity);
		
		// holy site deity not found
		deity = religions.GetDeityForHolySiteId(404);
		Assert.Null(deity);
	}
}