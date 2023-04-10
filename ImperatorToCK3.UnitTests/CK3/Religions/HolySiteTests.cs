using commonItems;
using commonItems.Colors;
using commonItems.Serialization;
using ImperatorToCK3.CK3.Religions;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.Mappers.HolySiteEffect;
using System.Collections.Generic;
using Xunit;

namespace ImperatorToCK3.UnitTests.CK3.Religions;

public class HolySiteTests {
	[Fact]
	public void PropertiesHaveCorrectInitialValues() {
		var site = new HolySite("test_site", new BufferedReader(), new Title.LandedTitles());
		Assert.Equal("test_site", site.Id);
		Assert.False(site.IsGeneratedByConverter);
		Assert.Null(site.CountyId);
		Assert.Null(site.BaronyId);
		Assert.Empty(site.CharacterModifier);
		Assert.Null(site.Flag);
	}

	[Fact]
	public void PropertiesAreCorrectlyRead() {
		var titles = new Title.LandedTitles();
		titles.LoadTitles(new BufferedReader("c_county = { b_barony = {} }"));
		var siteReader = new BufferedReader(@"
			county = c_county
			barony = b_barony
			character_modifier = {
				prowess = script_value_1
				piety = 1.5
			}
			flag = jerusalem_conversion_bonus # +20% County Conversion
		");
		var site = new HolySite("test_site", siteReader, titles);

		Assert.Equal("test_site", site.Id);
		Assert.False(site.IsGeneratedByConverter);
		Assert.Equal("c_county", site.CountyId);
		Assert.Equal("b_barony", site.BaronyId);
		Assert.Collection(site.CharacterModifier,
			kvp1 => {
				Assert.Equal("prowess", kvp1.Key);
				Assert.Equal("script_value_1", kvp1.Value);
			},
			kvp2 => {
				Assert.Equal("piety", kvp2.Key);
				Assert.Equal("1.5", kvp2.Value);
			});
		Assert.Equal("jerusalem_conversion_bonus", site.Flag);
	}

	[Fact]
	public void HolySiteCanBeConstructedForBaronyAndFaithWithEffects() {
		var titles = new Title.LandedTitles();
		var titlesReader = new BufferedReader("c_county = { b_barony = { province = 1 } }");
		titles.LoadTitles(titlesReader);

		var holySiteEffectMapper = new HolySiteEffectMapper("TestFiles/configurables/holy_site_effect_mappings.txt");
		var imperatorEffects = new Dictionary<string, double> {
			{"discipline", 0.2f}, // will be converted to knight_effectiveness_mult with factor of 10
			{"unmapped_effect", 1f} // will be skipped
		};
		
		var religions = new ReligionCollection(new Title.LandedTitles());
		var testReligion = new Religion("test_religion", new BufferedReader("{}"), religions, new ColorFactory());
		var faith = new Faith("test_faith", new BufferedReader(), testReligion, new ColorFactory());

		var site = new HolySite(titles["b_barony"], faith, titles, imperatorEffects, holySiteEffectMapper);

		Assert.Equal("IRtoCK3_b_barony_test_faith", site.Id);
		Assert.True(site.IsGeneratedByConverter);
		Assert.Equal("c_county", site.CountyId);
		Assert.Equal("b_barony", site.BaronyId);
		Assert.Collection(site.CharacterModifier,
			kvp1 => {
				Assert.Equal("knight_effectiveness_mult", kvp1.Key);
				Assert.Equal("2", PDXSerializer.Serialize(kvp1.Value)); // 0.2 * 10
			});
		Assert.Null(site.Flag);
	}
}