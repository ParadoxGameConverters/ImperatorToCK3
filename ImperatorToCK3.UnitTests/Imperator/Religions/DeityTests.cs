using commonItems;
using commonItems.Mods;
using ImperatorToCK3.Imperator;
using ImperatorToCK3.Imperator.Religions;
using System.Collections.Generic;
using Xunit;

namespace ImperatorToCK3.UnitTests.Imperator.Religions; 

public class DeityTests {
	private const string ImperatorRoot = "TestFiles/Imperator/game";
	private static readonly List<Mod> mods = new();
	private readonly ModFilesystem imperatorModFS = new(ImperatorRoot, mods);

	[Fact]
	public void ConstructedDeityHasCorrectId() {
		var deity = new Deity("test_deity", new BufferedReader(), new ScriptValueCollection());
		
		Assert.Equal("test_deity", deity.Id);
	}

	[Fact]
	public void ModifiersAreRead() {
		var scriptValues = new ScriptValueCollection();
		scriptValues.LoadScriptValues(imperatorModFS);
		
		var deityReader = new BufferedReader(@"
			passive_modifier = {
				effect_1 = 5.4
				effect_2 = value1 # 0.4
			}
		");
		var deity = new Deity("deity1", deityReader, scriptValues);
		Assert.Equal(5.4f, deity.PassiveModifiers["effect_1"]);
		Assert.Equal(0.4f, deity.PassiveModifiers["effect_2"]);
	}
}