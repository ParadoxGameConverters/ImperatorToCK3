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
}