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
		
		Assert.Equal(6, scriptValueCollection.Count);
		
		scriptValueCollection.Keys.Should().BeEquivalentTo("value1", "value2", "value3", "value4", "mod_value", "common_value");
		scriptValueCollection.Values.Should().BeEquivalentTo(new List<double>{0.4d, -0.4d, 1d, -3d, 3.2d, 69d});
		
		Assert.Equal(0.4d, scriptValueCollection["value1"]);
		Assert.Equal(-0.4d, scriptValueCollection["value2"]);
		Assert.Equal(1d, scriptValueCollection["value3"]);
		Assert.Equal(-3d, scriptValueCollection["value4"]);
		Assert.Equal(3.2d, scriptValueCollection["mod_value"]);
		Assert.Equal(69d, scriptValueCollection["common_value"]); // 68 in game, overridden by 69 in mod
	}

	[Fact]
	public void IndexerThrowsExceptionWhenKeyIsNotFound() {
		var scriptValueCollection = new ScriptValueCollection();
		Assert.Throws<KeyNotFoundException>(() => _ = scriptValueCollection["missing_key"]);
	}

	[Fact]
	public void GetValueForStringReturnsNumberForValidNumberString() {
		var scriptValueCollection = new ScriptValueCollection();
		
		Assert.Equal(5.5f, scriptValueCollection.GetValueForString("5.5"));
	}

	[Fact]
	public void GetValueForStringReturnsNullForInvalidNumberString() {
		var scriptValueCollection = new ScriptValueCollection();
		const string invalidNumberString = "2e";
		
		Assert.Null(scriptValueCollection.GetValueForString(invalidNumberString));
	}

	[Fact]
	public void GetValueForStringReturnsNumberForExistingScriptValue() {
		var scriptValueCollection = new ScriptValueCollection();
		scriptValueCollection.LoadScriptValues(imperatorModFS);
		
		Assert.Equal(0.4d, scriptValueCollection.GetValueForString("value1"));
	}

	[Fact]
	public void GetValueForStringReturnsNullForUndefinedScriptValue() {
		var scriptValueCollection = new ScriptValueCollection();
		scriptValueCollection.LoadScriptValues(imperatorModFS);
		
		Assert.Null(scriptValueCollection.GetValueForString("undefined_value"));
	}

	[Fact]
	public void ContainsKeyReturnsCorrectValues() {
		var scriptValueCollection = new ScriptValueCollection();
		scriptValueCollection.LoadScriptValues(imperatorModFS);
		
		Assert.True(scriptValueCollection.ContainsKey("value1"));
		Assert.True(scriptValueCollection.ContainsKey("mod_value"));
		Assert.True(scriptValueCollection.ContainsKey("common_value"));
		
		Assert.False(scriptValueCollection.ContainsKey("missing_value"));
	}

	[Fact]
	public void Values() {
		var scriptValueCollection = new ScriptValueCollection();
		scriptValueCollection.LoadScriptValues(imperatorModFS);

		// value4 should be found when iterating over the collection
		bool value4Found = false;
		using var enumerator = scriptValueCollection.GetEnumerator();
		foreach(var (key, value) in scriptValueCollection) {
			if (key != "value4") {
				continue;
			}

			value4Found = true;
			Assert.Equal(-3d, value);
		}
		
		Assert.True(value4Found);
	}
}