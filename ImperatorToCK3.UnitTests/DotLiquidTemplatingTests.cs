using commonItems;
using DotLiquid;
using System.Collections.Generic;
using System.Globalization;
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
	public void LiquidTemplateWorksCorrectly(bool wtwsms, bool tfe, bool vanillaCk3, int expectedValue1,
		int expectedValue2) {
		var ck3ModFlags = new Dictionary<string, bool> {["wtwsms"] = wtwsms, ["tfe"] = tfe, ["vanilla_ck3"] = vanillaCk3,};

		var template = Template.Parse(
			"""
				{% if wtwsms %}
					value1 = 0
				{% elsif tfe %}
					value1 = 1
				{% else %}
					value1 = 2
				{% endif %}
				
				{% if wtwsms or vanilla_ck3 or tfe %}
					value2 = 0
				{% endif %}
				{% if tfe %}
					value2 = 1
				{% endif %}
			""");

		// Check if constructing context from an anonymous object works.
		var context = Hash.FromAnonymousObject(new {wtwsms, tfe, vanilla_ck3 = vanillaCk3});
		var result = template.Render(context, CultureInfo.InvariantCulture);

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
		result = template.Render(context, CultureInfo.InvariantCulture);
		
		value1 = null;
		value2 = null;
		parser.ParseStream(new BufferedReader(result));
		
		Assert.Equal(expectedValue1, value1);
		Assert.Equal(expectedValue2, value2);
	}

	[Fact]
	public void ConverterOptionsCanBeUsedInLiquidTemplates() {
		var config = new Configuration {
			HeresiesInHistoricalAreas = false,
			FillerDukes = true,
			StaticDeJure = false,
			LegionConversion = LegionConversion.MenAtArms,
			ImperatorCurrencyRate = 0.67f,
			ImperatorCivilizationWorth = 0.34f,
			CK3BookmarkDate = new Date(867, 1, 1),
		};

		var liquidFlags = config.GetLiquidVariables();
		
		Assert.Equal("no", liquidFlags["HeresiesInHistoricalAreas"]);
		Assert.Equal("duke", liquidFlags["FillerDukes"]);
		Assert.Equal("dynamic", liquidFlags["StaticDeJure"]);
		Assert.Equal("men_at_arms", liquidFlags["LegionConversion"]);
		Assert.Equal(0.67f, (float)liquidFlags["ImperatorCurrencyRate"], precision: 2);
		Assert.Equal(0.34d, (double)liquidFlags["ImperatorCivilizationWorth"], precision: 2);
		Assert.Equal("0867-01-01", liquidFlags["bookmark_date"]);
		
		Template template = Template.Parse(
			"""
				{% if FillerDukes == 'duke' %}
					filler_rank = duke
				{% elsif FillerDukes == 'count' %}
					filler_rank = count
				{% endif %}
				
				{% if StaticDeJure == 'dynamic' %}
					de_jure = dynamic
				{% elsif StaticDeJure == 'static' %}
					de_jure = static
				{% endif %}
				
				{% if LegionConversion == 'men_at_arms' %}
					legions = maa
				{% elsif LegionConversion == 'special_troops' %}
					legions = special
				{% elsif LegionConversion == 'no' %}
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

				# test for embedding the value directly in the output
				set_civ_worth = {{ ImperatorCivilizationWorth }} }
				set_hist_heresies_str = "{{ HeresiesInHistoricalAreas }}"
			""");
		
		Hash context = config.GetLiquidVariables();
		string result = template.Render(context, CultureInfo.InvariantCulture);
		
		string? fillerRank = null;
		string? deJure = null;
		string? legions = null;
		string? currencyRate = null;
		string? bookmarkCentury = null;
		double? civWorth = null;
		string? historicalHeresiesStr = null;
		
		var parser = new Parser();
		parser.RegisterKeyword("filler_rank", reader => fillerRank = reader.GetString());
		parser.RegisterKeyword("de_jure", reader => deJure = reader.GetString());
		parser.RegisterKeyword("legions", reader => legions = reader.GetString());
		parser.RegisterKeyword("currency_rate", reader => currencyRate = reader.GetString());
		parser.RegisterKeyword("bookmark_century", reader => bookmarkCentury = reader.GetString());
		parser.RegisterKeyword("set_civ_worth", reader => civWorth = reader.GetDouble());
		parser.RegisterKeyword("set_hist_heresies_str", reader => historicalHeresiesStr = reader.GetString());
		parser.ParseStream(new BufferedReader(result));
		
		Assert.Equal("duke", fillerRank);
		Assert.Equal("dynamic", deJure);
		Assert.Equal("maa", legions);
		Assert.Equal("high", currencyRate);
		Assert.Equal("ninth", bookmarkCentury);
		Assert.NotNull(civWorth);
		Assert.Equal(0.34d, civWorth.Value, precision: 2);
		Assert.Equal("no", historicalHeresiesStr);
	}
}