using commonItems;
using DotLiquid;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace ImperatorToCK3.UnitTests;

[Collection("Sequential")]
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
			LegionConversion = LegionConversion.MenAtArms,
			ImperatorCurrencyRate = 0.67f,
			CK3BookmarkDate = new Date(867, 1, 1)
		};

		var liquidFlags = config.GetLiquidVariables();
		
		Assert.Equal("1", liquidFlags["FillerDukes"]);
		Assert.Equal("1", liquidFlags["StaticDeJure"]);
		Assert.Equal("2", liquidFlags["LegionConversion"]);
		Assert.Equal(0.67, (float)liquidFlags["ImperatorCurrencyRate"], precision: 2);
		Assert.Equal("0867-01-01", liquidFlags["bookmark_date"]);
		
		Template template = Template.Parse(
			"""
				{% if FillerDukes == '1' %}
					filler_rank = duke
				{% elsif FillerDukes == '0' %}
					filler_rank = count
				{% endif %}
				
				{% if StaticDeJure == '1' %}
					de_jure = dynamic
				{% elsif StaticDeJure == '2' %}
					de_jure = static
				{% endif %}
				
				{% if LegionConversion == '2' %}
					legions = maa
				{% elsif LegionConversion == '1' %}
					legions = special
				{% elsif LegionConversion == '0' %}
					legions = none
				{% endif %}
				
				{% if ImperatorCurrencyRate > 0.5 %}
					currency_rate = high
				{% else %}
					currency_rate = low
				{% endif %}
				
				{% if bookmark_date < '1001-01-01' and bookmark_date >= '0901-01-01' %}
					bookmark_century = tenth
				{% elsif bookmark_date < '0901-01-01' and bookmark_date >= '0801-01-01' %}
					bookmark_century = ninth
				{% elsif bookmark_date < '0801-01-01' and bookmark_date >= '0701-01-01' %}
					bookmark_century = eighth
				{% else %}
					bookmark_century = unexpected
				{% endif %}
			""");
		
		Hash context = config.GetLiquidVariables();
		string result = template.Render(context);
		
		string? fillerRank = null;
		string? deJure = null;
		string? legions = null;
		string? currencyRate = null;
		string? bookmarkCentury = null;
		
		var parser = new Parser();
		parser.RegisterKeyword("filler_rank", reader => fillerRank = reader.GetString());
		parser.RegisterKeyword("de_jure", reader => deJure = reader.GetString());
		parser.RegisterKeyword("legions", reader => legions = reader.GetString());
		parser.RegisterKeyword("currency_rate", reader => currencyRate = reader.GetString());
		parser.RegisterKeyword("bookmark_century", reader => bookmarkCentury = reader.GetString());
		parser.ParseStream(new BufferedReader(result));
		
		Assert.Equal("duke", fillerRank);
		Assert.Equal("dynamic", deJure);
		Assert.Equal("maa", legions);
		Assert.Equal("high", currencyRate);
		Assert.Equal("ninth", bookmarkCentury);
	}
}