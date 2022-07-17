using commonItems.Mods;
using FluentAssertions;
using ImperatorToCK3.Imperator;
using System;
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
		scriptValueCollection.Values.Should().BeEquivalentTo(new List<double>{0.4d, -0.4d, 1d, -3d, 3.2d});
		
		Assert.Equal(0.4d, scriptValueCollection["value1"]);
		Assert.Equal(-0.4d, scriptValueCollection["value2"]);
		Assert.Equal(1d, scriptValueCollection["value3"]);
		Assert.Equal(-3d, scriptValueCollection["value4"]);
		Assert.Equal(3.2d, scriptValueCollection["mod_value"]);
	}

	[Fact]
	public void GetModifierValueReturnsNumberForValidNumberString() {
		var scriptValueCollection = new ScriptValueCollection();
		
		Assert.Equal(5.5f, scriptValueCollection.GetModifierValue("5.5"));
	}

	[Fact]
	public void GetModifierValueReturnsDefaultValueForInvalidNumberString() {
		var scriptValueCollection = new ScriptValueCollection();
		const string invalidNumberString = "2e";
		
		// default value is 1
		Assert.Equal(1d, scriptValueCollection.GetModifierValue(invalidNumberString));
	}

	[Fact]
	public void GetModifierValueReturnsNumberForExistingScriptValue() {
		var scriptValueCollection = new ScriptValueCollection();
		scriptValueCollection.LoadScriptValues(imperatorModFS);
		
		Assert.Equal(0.4d, scriptValueCollection.GetModifierValue("value1"));
	}

	[Fact]
	public void GetModifierValueReturnsDefaultValueForUndefinedScriptValue() {
		var scriptValueCollection = new ScriptValueCollection();
		scriptValueCollection.LoadScriptValues(imperatorModFS);
		
		// default value is 1
		Assert.Equal(1f, scriptValueCollection.GetModifierValue("undefined_value"));
	}
}