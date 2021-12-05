using commonItems;
using ImperatorToCK3.Mappers.Trait;
using Xunit;

namespace ImperatorToCK3.UnitTests.Mappers.Trait {
	public class TraitMapperTests {
		[Fact]
		public void NonMatchGivesEmptyOptional() {
			var reader = new BufferedReader("link = { ck3 = ck3Trait imp = impTrait }");
			var mapper = new TraitMapper(reader);

			var ck3Trait = mapper.GetCK3TraitForImperatorTrait("nonMatchingTrait");
			Assert.Null(ck3Trait);
		}

		[Fact]
		public void Ck3TraitCanBeFound() {
			var reader = new BufferedReader("link = { ck3 = ck3Trait imp = impTrait }");
			var mapper = new TraitMapper(reader);

			var ck3Trait = mapper.GetCK3TraitForImperatorTrait("impTrait");
			Assert.Equal("ck3Trait", ck3Trait);
		}

		[Fact]
		public void MultipleImpTraitsCanBeInARule() {
			var reader = new BufferedReader("link = { ck3 = ck3Trait imp = impTrait imp = impTrait2 }");
			var mapper = new TraitMapper(reader);

			var ck3Trait = mapper.GetCK3TraitForImperatorTrait("impTrait2");
			Assert.Equal("ck3Trait", ck3Trait);
		}

		[Fact]
		public void CorrectRuleMatches() {
			var reader = new BufferedReader(
				"link = { ck3 = ck3Trait imp = impTrait }" +
				"link = { ck3 = ck3Trait2 imp = impTrait2 }"
			);
			var mapper = new TraitMapper(reader);

			var ck3Trait = mapper.GetCK3TraitForImperatorTrait("impTrait2");
			Assert.Equal("ck3Trait2", ck3Trait);
		}

		[Fact]
		public void MappingsAreReadFromFile() {
			var mapper = new TraitMapper("TestFiles/configurables/trait_map.txt");
			Assert.Equal("dull", mapper.GetCK3TraitForImperatorTrait("dull"));
			Assert.Equal("dull", mapper.GetCK3TraitForImperatorTrait("stupid"));
			Assert.Equal("kind", mapper.GetCK3TraitForImperatorTrait("friendly"));
			Assert.Equal("brave", mapper.GetCK3TraitForImperatorTrait("brave"));
		}

		[Fact]
		public void MappingsWithNoCK3TraitAreIgnored() {
			var reader = new BufferedReader(
				"link = { imp = impTrait }"
			);
			var mapper = new TraitMapper(reader);

			var ck3Trait = mapper.GetCK3TraitForImperatorTrait("impTrait");
			Assert.Null(ck3Trait);
		}
	}
}
