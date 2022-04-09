using commonItems;
using commonItems.Collections;
using ImperatorToCK3.Mappers.Trait;
using System.Collections.Generic;
using Xunit;

namespace ImperatorToCK3.UnitTests.Mappers.Trait;

public class TraitMapperTests {
	public class TestTraitMapper : TraitMapper {
		public TestTraitMapper(Dictionary<string, string> impToCK3TraitMap, IdObjectCollection<string, ImperatorToCK3.CK3.Characters.Trait> ck3Traits) : base() {
			this.ImpToCK3TraitMap = impToCK3TraitMap;
			this.CK3Traits = ck3Traits;
		}
	}
	[Fact]
	public void NonMatchGivesEmptyOptional() {
		var ck3Traits = new IdObjectCollection<string, ImperatorToCK3.CK3.Characters.Trait> {new("ck3Trait")};
		var traitsDict = new Dictionary<string, string> {{"impTrait", "ck3Trait"}};
		var mapper = new TestTraitMapper(traitsDict, ck3Traits);

		var ck3Trait = mapper.GetCK3TraitForImperatorTrait("nonMatchingTrait");
		Assert.Null(ck3Trait);
	}

	[Fact]
	public void Ck3TraitCanBeFound() {
		var ck3Traits = new IdObjectCollection<string, ImperatorToCK3.CK3.Characters.Trait> { new("ck3Trait") };
		var traitsDict = new Dictionary<string, string> { { "impTrait", "ck3Trait" } };
		var mapper = new TestTraitMapper(traitsDict, ck3Traits);

		var ck3Trait = mapper.GetCK3TraitForImperatorTrait("impTrait");
		Assert.Equal("ck3Trait", ck3Trait);
	}

	[Fact]
	public void MultipleImpTraitsCanBeInARule() {
		var mapper = new TraitMapper("TestFiles/TraitMapperTests/trait_map_multiple_ck3_in_rule.txt", new Configuration() { CK3Path = "TestFiles/CK3" });

		var ck3Trait = mapper.GetCK3TraitForImperatorTrait("impTrait2");
		Assert.Equal("ck3Trait", ck3Trait);
	}

	[Fact]
	public void CorrectRuleMatches() {
		var ck3Traits = new IdObjectCollection<string, ImperatorToCK3.CK3.Characters.Trait> { new("ck3Trait"), new("ck3Trait2") };
		var traitsDict = new Dictionary<string, string> {
			{ "impTrait", "ck3Trait" },
			{"impTrait2", "ck3Trait2"}
		};
		var mapper = new TestTraitMapper(traitsDict, ck3Traits);

		var ck3Trait = mapper.GetCK3TraitForImperatorTrait("impTrait2");
		Assert.Equal("ck3Trait2", ck3Trait);
	}

	[Fact]
	public void MappingsAreReadFromFile() {
		var mapper = new TraitMapper("TestFiles/configurables/trait_map.txt", new Configuration() { CK3Path = "TestFiles/CK3" });
		Assert.Equal("dull", mapper.GetCK3TraitForImperatorTrait("dull"));
		Assert.Equal("dull", mapper.GetCK3TraitForImperatorTrait("stupid"));
		Assert.Equal("kind", mapper.GetCK3TraitForImperatorTrait("friendly"));
		Assert.Equal("brave", mapper.GetCK3TraitForImperatorTrait("brave"));
	}

	[Fact]
	public void MappingsWithNoCK3TraitAreIgnored() {
		var mapper = new TraitMapper("TestFiles/TraitMapperTests/trait_map_mapping_with_no_ck3.txt", new Configuration() { CK3Path = "TestFiles/CK3" });

		var ck3Trait = mapper.GetCK3TraitForImperatorTrait("impTrait");
		Assert.Null(ck3Trait);
	}

	[Fact]
	public void OppositeTraitsAreNotReturned() {
		var mapper = new TraitMapper("TestFiles/configurables/trait_map.txt", new Configuration() { CK3Path = "TestFiles/CK3" });

		// when checked separately, mapper returns both
		Assert.Equal("wise", mapper.GetCK3TraitForImperatorTrait("wise"));
		Assert.Equal("dumb", mapper.GetCK3TraitForImperatorTrait("dumb"));

		// when mapping a set of traits, opposites are not returned
		var impTraits = new List<string> { "wise", "dumb" };
		Assert.Collection(mapper.GetCK3TraitsForImperatorTraits(impTraits),
			trait => Assert.Equal("wise", trait)
		);
	}
}