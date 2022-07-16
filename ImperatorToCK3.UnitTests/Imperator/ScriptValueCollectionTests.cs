using commonItems.Mods;
using FluentAssertions;
using ImperatorToCK3.Imperator;
using System.Collections.Generic;
using Xunit;

namespace ImperatorToCK3.UnitTests.Imperator; 

public class ScriptValueCollectionTests {
	private const string ImperatorRoot = "TestFiles/Imperator/game";
	private static readonly List<Mod> mods = new() { new("mod1", "TestFiles/documents/Imperator/mod/cool_mod") };
	private readonly ModFilesystem imperatorModFS = new(ImperatorRoot, mods);
	
	[Fact]
	public void ScriptValuesAreReadFromGameAndMods() {
		var scriptValueCollection = new ScriptValueCollection();
		scriptValueCollection.LoadScriptValues(imperatorModFS);
		
		Assert.Equal(5, scriptValueCollection.Count);
		
		scriptValueCollection.Keys.Should().BeEquivalentTo("value1", "value2", "value3", "value4", "mod_value");
		scriptValueCollection.Values.Should().BeEquivalentTo(new List<float>{0.4f, -0.4f, 1f, -3f, 3.2f});
		
		Assert.Equal(0.4f, scriptValueCollection["value1"]);
		Assert.Equal(-0.4f, scriptValueCollection["value2"]);
		Assert.Equal(1f, scriptValueCollection["value3"]);
		Assert.Equal(-3f, scriptValueCollection["value4"]);
		Assert.Equal(3.2f, scriptValueCollection["mod_value"]);
	}
}