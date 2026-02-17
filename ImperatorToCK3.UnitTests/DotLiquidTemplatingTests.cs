using commonItems;
using DotLiquid;
using Open.Collections;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace ImperatorToCK3.UnitTests;

public class DotLiquidTemplatingTests {
	[Theory]
	[InlineData(true, false, false, 0, 0)]
	[InlineData(false, true, false, 1, 1)]
	[InlineData(false, false, true, 2, 0)]
	[InlineData(true, true, false, 0, 1)]
	public void LiquidTemplateWorksCorrectly(bool wtwsms, bool tfe, bool vanilla, int expectedValue1,
		int expectedValue2) {
		var ck3ModFlags = new Dictionary<string, bool> {["wtwsms"] = wtwsms, ["tfe"] = tfe, ["vanilla"] = vanilla,};

		var template = Template.Parse(
			"""
				{% if wtwsms %}
					value1 = 0
				{% elsif tfe %}
					value1 = 1
				{% else %}
					value1 = 2
				{% endif %}
				
				{% if wtwsms or vanilla or tfe %}
					value2 = 0
				{% endif %}
				{% if tfe %}
					value2 = 1
				{% endif %}
			""");

		// Check if constructing context from an anonymous object works.
		var context = Hash.FromAnonymousObject(new {wtwsms, tfe, vanilla});
		var result = template.Render(context);

		int? value1 = null;
		int? value2 = null;
		var parser = new Parser();
		parser.RegisterKeyword("value1", reader => value1 = reader.GetInt());
		parser.RegisterKeyword("value2", reader => value2 = reader.GetInt());
		parser.ParseStream(new BufferedReader(result));

		Assert.Equal(expectedValue1, value1);
		Assert.Equal(expectedValue2, value2);
		
		// Check if constructing context from a dictionary works.
		// The dictionary needs to be converted to a dictionary of objects first.
		context = Hash.FromDictionary(ck3ModFlags.ToDictionary(pair => pair.Key, pair => (object)pair.Value));
		result = template.Render(context);
		
		value1 = null;
		value2 = null;
		parser.ParseStream(new BufferedReader(result));
		
		Assert.Equal(expectedValue1, value1);
		Assert.Equal(expectedValue2, value2);
	}

	[Fact]
	public void ConverterOptionsCanBeUsedInLiquidTemplates() {
		var config = new Configuration {
			FillerDukes = true,
			StaticDeJure = false,
			LegionConversion = LegionConversion.MenAtArms
		};

		var converterOptions = config.GetConverterOptions();
		
		// FillerDukes = true means choice 1 is selected
		Assert.True(converterOptions["FillerDukes_1"]);
		Assert.False(converterOptions["FillerDukes_0"]);
		
		// StaticDeJure = false means choice 1 is selected (dynamic)
		Assert.True(converterOptions["StaticDeJure_1"]);
		Assert.False(converterOptions["StaticDeJure_2"]);
		
		// LegionConversion = MenAtArms
		Assert.True(converterOptions["LegionConversion_MenAtArms"]);
		Assert.False(converterOptions["LegionConversion_No"]);
		Assert.False(converterOptions["LegionConversion_SpecialTroops"]);
		
		// Test that these options work in liquid templates
		var template = Template.Parse(
			"""
				{% if FillerDukes_1 %}
					filler_rank = duke
				{% elsif FillerDukes_0 %}
					filler_rank = count
				{% endif %}
				
				{% if StaticDeJure_1 %}
					dejure = dynamic
				{% elsif StaticDeJure_2 %}
					dejure = static
				{% endif %}
				
				{% if LegionConversion_MenAtArms %}
					legions = maa
				{% elsif LegionConversion_SpecialTroops %}
					legions = special
				{% elsif LegionConversion_No %}
					legions = none
				{% endif %}
			""");
		
		var convertedOptions = converterOptions.ToDictionary(kv => kv.Key, kv => (object)kv.Value);
		var context = Hash.FromDictionary(convertedOptions);
		var result = template.Render(context);
		
		string? fillerRank = null;
		string? dejure = null;
		string? legions = null;
		var parser = new Parser();
		parser.RegisterKeyword("filler_rank", reader => fillerRank = reader.GetString());
		parser.RegisterKeyword("dejure", reader => dejure = reader.GetString());
		parser.RegisterKeyword("legions", reader => legions = reader.GetString());
		parser.ParseStream(new BufferedReader(result));
		
		Assert.Equal("duke", fillerRank);
		Assert.Equal("dynamic", dejure);
		Assert.Equal("maa", legions);
	}

	[Fact]
	public void ConverterOptionsWithDifferentValues() {
		var config = new Configuration {
			FillerDukes = false,
			StaticDeJure = true,
			LegionConversion = LegionConversion.No
		};

		var converterOptions = config.GetConverterOptions();
		
		// FillerDukes = false means choice 0 is selected
		Assert.True(converterOptions["FillerDukes_0"]);
		Assert.False(converterOptions["FillerDukes_1"]);
		
		// StaticDeJure = true means choice 2 is selected (static)
		Assert.True(converterOptions["StaticDeJure_2"]);
		Assert.False(converterOptions["StaticDeJure_1"]);
		
		// LegionConversion = No
		Assert.True(converterOptions["LegionConversion_No"]);
		Assert.False(converterOptions["LegionConversion_MenAtArms"]);
		Assert.False(converterOptions["LegionConversion_SpecialTroops"]);
	}
}