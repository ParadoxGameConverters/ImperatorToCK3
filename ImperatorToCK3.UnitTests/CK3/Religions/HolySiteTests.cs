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
	private static readonly ColorFactory colorFactory = new();
	
	[Fact]
	public void PropertiesHaveCorrectInitialValues() {
		var site = new HolySite("test_site", new BufferedReader(), new Title.LandedTitles(), isFromConverter: false);
		Assert.Equal("test_site", site.Id);
		Assert.False(site.IsFromConverter);
		Assert.Null(site.CountyId);
		Assert.Null(site.BaronyId);
		Assert.Empty(site.CharacterModifier);
		Assert.Null(site.Flag);
	}

	[Fact]
	public void PropertiesAreCorrectlyRead() {
		var titles = new Title.LandedTitles();
		titles.LoadTitles(new BufferedReader("c_county = { b_barony = {} }"), colorFactory);
		var siteReader = new BufferedReader(@"
			county = c_county
			barony = b_barony
			character_modifier = {
				prowess = script_value_1
				piety = 1.5
			}
			flag = jerusalem_conversion_bonus # +20% County Conversion
		");
		var site = new HolySite("test_site", siteReader, titles, isFromConverter: false);

		Assert.Equal("test_site", site.Id);
		Assert.False(site.IsFromConverter);
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
		titles.LoadTitles(titlesReader, colorFactory);

		var holySiteEffectMapper = new HolySiteEffectMapper("TestFiles/configurables/holy_site_effect_mappings.txt");
		var imperatorEffects = new OrderedDictionary<string, double> {
			{"discipline", 0.2f}, // will be converted to knight_effectiveness_mult with factor of 10
			{"unmapped_effect", 1f}, // will be skipped
		};
		
		var religions = new ReligionCollection(new Title.LandedTitles());
		var testReligion = new Religion("test_religion", new BufferedReader("{}"), religions, colorFactory);
		var faith = new Faith("test_faith", new FaithData(), testReligion);

		var site = new HolySite(titles["b_barony"], faith, titles, imperatorEffects, holySiteEffectMapper);

		Assert.Equal("IRtoCK3_b_barony_test_faith", site.Id);
		Assert.True(site.IsFromConverter);
		Assert.Equal("c_county", site.CountyId);
		Assert.Equal("b_barony", site.BaronyId);
		Assert.Collection(site.CharacterModifier,
			kvp1 => {
				Assert.Equal("knight_effectiveness_mult", kvp1.Key);
				Assert.Equal("2", PDXSerializer.Serialize(kvp1.Value)); // 0.2 * 10
			});
		Assert.Null(site.Flag);
	}

	[Fact]
	public void CountyChoicesAreUsedToPickFirstExistingCounty() {
		var titles = new Title.LandedTitles();
		titles.LoadTitles(new BufferedReader("c_county = { b_barony = {} }"), colorFactory);
		var siteReader = new BufferedReader(@"
			county_choices = { c_nonexistent1 c_county c_nonexistent2 }
			character_modifier = {}
		");
		var site = new HolySite("test_site", siteReader, titles, isFromConverter: true);
		
		Assert.Equal("c_county", site.CountyId);
	}

	[Fact]
	public void BaronyCountyMismatchIsFixedWhenCorrectCountyExists() {
		var titles = new Title.LandedTitles();
		// Create a structure where barony belongs to correct_county, but site specifies wrong_county
		titles.LoadTitles(new BufferedReader(@"
			c_correct_county = { 
				b_test_barony = {} 
			}
			c_wrong_county = {}
		"), colorFactory);
		
		var siteReader = new BufferedReader(@"
			county = c_wrong_county
			barony = b_test_barony
			character_modifier = {}
		");
		
		var site = new HolySite("test_site", siteReader, titles, isFromConverter: false);
		
		// County should be corrected to the barony's de jure liege
		Assert.Equal("c_correct_county", site.CountyId);
		Assert.Equal("b_test_barony", site.BaronyId);
	}

	[Fact]
	public void BaronyCountyMismatchIsNotFixedWhenBaronyHasNoDeJureLiege() {
		var titles = new Title.LandedTitles();
		// Create a barony without a parent county and a separate county
		titles.LoadTitles(new BufferedReader(@"
			b_orphan_barony = {}
			c_some_county = {}
		"), colorFactory);
		
		var siteReader = new BufferedReader(@"
			county = c_some_county
			barony = b_orphan_barony
			character_modifier = {}
		");
		
		var site = new HolySite("test_site", siteReader, titles, isFromConverter: false);
		
		// County should remain unchanged since barony has no de jure liege
		Assert.Equal("c_some_county", site.CountyId);
		Assert.Equal("b_orphan_barony", site.BaronyId);
	}

	[Fact]
	public void BaronyCountyMatchingIsNotChangedWhenAlreadyCorrect() {
		var titles = new Title.LandedTitles();
		// Create a structure where barony correctly belongs to the specified county
		titles.LoadTitles(new BufferedReader(@"
			c_correct_county = { 
				b_test_barony = {} 
			}
		"), colorFactory);
		
		var siteReader = new BufferedReader(@"
			county = c_correct_county
			barony = b_test_barony
			character_modifier = {}
		");
		
		var site = new HolySite("test_site", siteReader, titles, isFromConverter: false);
		
		// County should remain unchanged since it's already correct
		Assert.Equal("c_correct_county", site.CountyId);
		Assert.Equal("b_test_barony", site.BaronyId);
	}
}