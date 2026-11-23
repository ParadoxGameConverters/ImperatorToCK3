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
		Assert.False(site.IsDynamic);
		Assert.Empty(site.Parameters);
		Assert.Empty(site.FaithCharacterModifier);
		Assert.Empty(site.FaithCharacterModifierScaled);
		Assert.Empty(site.CountyHolderCharacterModifier);
		Assert.Empty(site.CountyModifierScaled);
	}

	[Fact]
	public void PropertiesAreCorrectlyRead() {
		var titles = new Title.LandedTitles();
		titles.LoadTitles(new BufferedReader("c_county = { b_barony = {} }"), colorFactory);
		var siteReader = new BufferedReader(@"
			county = c_county
			barony = b_barony
			parameters = { jerusalem_conversion_bonus }
			county_holder_character_modifier = {
				prowess = script_value_1
				piety = 1.5
			}
			faith_character_modifier = {
				monthly_piety_gain_mult = 0.05
			}
		");
		var site = new HolySite("test_site", siteReader, titles, isFromConverter: false);

		Assert.Equal("test_site", site.Id);
		Assert.False(site.IsFromConverter);
		Assert.Equal("c_county", site.CountyId);
		Assert.Equal("b_barony", site.BaronyId);
		Assert.Equal(["jerusalem_conversion_bonus"], site.Parameters);
		Assert.Collection(site.CountyHolderCharacterModifier,
			kvp1 => {
				Assert.Equal("prowess", kvp1.Key);
				Assert.Equal("script_value_1", kvp1.Value);
			},
			kvp2 => {
				Assert.Equal("piety", kvp2.Key);
				Assert.Equal("1.5", kvp2.Value);
			});
		Assert.Collection(site.FaithCharacterModifier,
			kvp => {
				Assert.Equal("monthly_piety_gain_mult", kvp.Key);
				Assert.Equal("0.05", kvp.Value);
			});
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
		var testReligion = new Religion("test_religion", new BufferedReader("{}"), religions);
		var faith = new Faith("test_faith", new FaithData(), testReligion);

		var site = new HolySite(titles["b_barony"], faith, titles, imperatorEffects, holySiteEffectMapper);

		Assert.Equal("IRtoCK3_b_barony_test_faith", site.Id);
		Assert.True(site.IsFromConverter);
		Assert.Equal("c_county", site.CountyId);
		Assert.Equal("b_barony", site.BaronyId);
		Assert.Collection(site.FaithCharacterModifier,
			kvp1 => {
				Assert.Equal("knight_effectiveness_mult", kvp1.Key);
				Assert.Equal("2", PDXSerializer.Serialize(kvp1.Value)); // 0.2 * 10
			});
		Assert.Collection(site.CountyHolderCharacterModifier,
			kvp1 => {
				Assert.Equal("knight_effectiveness_mult", kvp1.Key);
				Assert.Equal("2", PDXSerializer.Serialize(kvp1.Value));
			});
	}

	[Fact]
	public void BlockFormatEffectMappingsAreAppliedToTheirRespectiveBlocks() {
		var titles = new Title.LandedTitles();
		var titlesReader = new BufferedReader("c_county = { b_barony = { province = 1 } }");
		titles.LoadTitles(titlesReader, colorFactory);

		// Uses TestFiles/HolySiteEffectMapperTests/mappings.txt, which contains
		// a CK3 1.20+ block-format mapping for "block_effect".
		var holySiteEffectMapper = new HolySiteEffectMapper("TestFiles/HolySiteEffectMapperTests/mappings.txt");
		var imperatorEffects = new OrderedDictionary<string, double> {
			{"block_effect", 1f},
		};

		var religions = new ReligionCollection(new Title.LandedTitles());
		var testReligion = new Religion("test_religion", new BufferedReader("{}"), religions);
		var faith = new Faith("test_faith", new FaithData(), testReligion);

		var site = new HolySite(titles["b_barony"], faith, titles, imperatorEffects, holySiteEffectMapper);

		Assert.Collection(site.FaithCharacterModifier,
			kvp => {
				Assert.Equal("faith_mod", kvp.Key);
				Assert.Equal("10", PDXSerializer.Serialize(kvp.Value));
			});
		Assert.Collection(site.FaithCharacterModifierScaled,
			kvp1 => {
				Assert.Equal("scale", kvp1.Key);
				Assert.Equal("holy_site_total_artifact_rarity", kvp1.Value);
			},
			kvp2 => {
				Assert.Equal("faith_scaled_mod", kvp2.Key);
				Assert.Equal("0.2", PDXSerializer.Serialize(kvp2.Value));
			},
			kvp3 => {
				Assert.Equal("faith_scaled_mod2", kvp3.Key);
				Assert.Equal("0.4", PDXSerializer.Serialize(kvp3.Value));
			});
		Assert.Collection(site.CountyHolderCharacterModifier,
			kvp => {
				Assert.Equal("county_holder_mod", kvp.Key);
				Assert.Equal("5", PDXSerializer.Serialize(kvp.Value));
			});
		Assert.Collection(site.CountyModifierScaled,
			kvp1 => {
				Assert.Equal("scale", kvp1.Key);
				Assert.Equal("holy_site_total_artifact_rarity", kvp1.Value);
			},
			kvp2 => {
				Assert.Equal("county_scaled_mod", kvp2.Key);
				Assert.Equal("0.2", PDXSerializer.Serialize(kvp2.Value));
			});

		// The scale value must be serialized unquoted, like in vanilla CK3 holy sites.
		var serialized = PDXSerializer.Serialize(site);
		Assert.Contains("scale = holy_site_total_artifact_rarity", serialized);
	}

	[Fact]
	public void CountyChoicesAreUsedToPickFirstExistingCounty() {
		var titles = new Title.LandedTitles();
		titles.LoadTitles(new BufferedReader("c_county = { b_barony = {} }"), colorFactory);
		var siteReader = new BufferedReader(@"
			county_choices = { c_nonexistent1 c_county c_nonexistent2 }
			faith_character_modifier = {}
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
			faith_character_modifier = {}
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
			faith_character_modifier = {}
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
			faith_character_modifier = {}
		");
		
		var site = new HolySite("test_site", siteReader, titles, isFromConverter: false);
		
		// County should remain unchanged since it's already correct
		Assert.Equal("c_correct_county", site.CountyId);
		Assert.Equal("b_test_barony", site.BaronyId);
	}
}