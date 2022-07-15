using commonItems.Mods;
using ImperatorToCK3.Imperator;
using System.Collections.Generic;
using Xunit;

namespace ImperatorToCK3.UnitTests.Imperator; 

public class ScriptValueCollectionTests {
	[Fact]
	public void ScriptValuesAreReadFromGameAndMods() {
		const string imperatorRoot = "TestFiles/Imperator/game";
		var mods = new List<Mod> { new("mod1", "TestFiles/documents/Imperator/mod/cool_mod") };
		var imperatorModFS = new ModFilesystem(imperatorRoot, mods);
		var values = new ScriptValueCollection();
		values.LoadScriptValues(imperatorModFS);
		
		Assert.Equal(5, values.Count);
		Assert.Equal(0.4f, values["value1"]);
		Assert.Equal(-0.4f, values["value2"]);
		Assert.Equal(1f, values["value3"]);
		Assert.Equal(-3f, values["value4"]);
		Assert.Equal(3.2f, values["mod_value"]);
	}
}