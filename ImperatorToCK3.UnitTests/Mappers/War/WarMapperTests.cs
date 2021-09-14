using System.IO;
using ImperatorToCK3.Mappers.War;
using Xunit;

namespace ImperatorToCK3.UnitTests.Mappers.War {
	public class WarMapperTests {
		[Fact]
		public void NonMatchGivesNull() {
			const string tempTestFile = "TestFiles/configurables/temp_wargoal_map_1.txt";
			File.WriteAllText(tempTestFile, "link = { ck3 = ck3CB imp = goal }");
			var mapper = new WarMapper(tempTestFile);

			var ck3Trait = mapper.GetCK3CBForImperatorWarGoal("nonMatchingGoal");
			Assert.Null(ck3Trait);
		}

		[Fact]
		public void Ck3TraitCanBeFound() {
			const string tempTestFile = "TestFiles/configurables/temp_wargoal_map_2.txt";
			File.WriteAllText(tempTestFile, "link = { ck3 = ck3CB imp = goal }");
			var mapper = new WarMapper(tempTestFile);

			var ck3Trait = mapper.GetCK3CBForImperatorWarGoal("goal");
			Assert.Equal("ck3CB", ck3Trait);
		}

		[Fact]
		public void MultipleImpTraitsCanBeInARule() {
			const string tempTestFile = "TestFiles/configurables/temp_wargoal_map_3.txt";
			File.WriteAllText(tempTestFile, "link = { ck3=ck3CB imp=goal1 imp=goal2 }");
			var mapper = new WarMapper(tempTestFile);

			var ck3Trait = mapper.GetCK3CBForImperatorWarGoal("goal2");
			Assert.Equal("ck3CB", ck3Trait);
		}

		[Fact]
		public void CorrectRuleMatches() {
			const string tempTestFile = "TestFiles/configurables/temp_wargoal_map_4.txt";
			File.WriteAllText(tempTestFile,
				"link = { ck3 = ck3CB1 imp = goal1 }" +
				"link = { ck3 = ck3CB2 imp = goal2 }"
			);
			var mapper = new WarMapper(tempTestFile);

			var ck3Trait = mapper.GetCK3CBForImperatorWarGoal("goal2");
			Assert.Equal("ck3CB2", ck3Trait);
		}

		[Fact]
		public void MappingsAreReadFromFile() {
			var mapper = new WarMapper("TestFiles/configurables/wargoal_mappings.txt");
			Assert.Equal("independence_faction_war", mapper.GetCK3CBForImperatorWarGoal("independence_wargoal"));
			Assert.Equal("claim_cb", mapper.GetCK3CBForImperatorWarGoal("conquer_wargoal"));
			Assert.Equal("vassalization_cb", mapper.GetCK3CBForImperatorWarGoal("imperial_conquest_wargoal"));
			Assert.Equal("imperial_reconquest_cb", mapper.GetCK3CBForImperatorWarGoal("diadochi_wargoal"));
		}
	}
}
