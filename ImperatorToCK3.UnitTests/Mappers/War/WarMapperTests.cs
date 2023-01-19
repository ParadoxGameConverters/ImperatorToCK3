using System.IO;
using ImperatorToCK3.Mappers.War;
using Xunit;

namespace ImperatorToCK3.UnitTests.Mappers.War; 

[Collection("Sequential")]
[CollectionDefinition("Sequential", DisableParallelization = true)]
public class WarMapperTests {
	[Fact]
	public void NonMatchGivesNull() {
		const string tempTestFile = "TestFiles/configurables/temp_wargoal_map_1.txt";
		File.WriteAllText(tempTestFile, "link = { ck3 = ck3CB ir = goal }");
		var mapper = new WarMapper(tempTestFile);

		var ck3CB = mapper.GetCK3CBForImperatorWarGoal("nonMatchingGoal");
		Assert.Null(ck3CB);
	}

	[Fact]
	public void CK3CBCanBeFound() {
		const string tempTestFile = "TestFiles/configurables/temp_wargoal_map_2.txt";
		File.WriteAllText(tempTestFile, "link = { ck3 = ck3CB ir = goal }");
		var mapper = new WarMapper(tempTestFile);

		var ck3CB = mapper.GetCK3CBForImperatorWarGoal("goal");
		Assert.Equal("ck3CB", ck3CB);
	}

	[Fact]
	public void MultipleImperatorWarGoalsCanBeInARule() {
		const string tempTestFile = "TestFiles/configurables/temp_wargoal_map_3.txt";
		File.WriteAllText(tempTestFile, "link = { ck3=ck3CB ir=goal1 ir=goal2 }");
		var mapper = new WarMapper(tempTestFile);

		var ck3CB = mapper.GetCK3CBForImperatorWarGoal("goal2");
		Assert.Equal("ck3CB", ck3CB);
	}

	[Fact]
	public void CorrectRuleMatches() {
		const string tempTestFile = "TestFiles/configurables/temp_wargoal_map_4.txt";
		File.WriteAllText(tempTestFile,
			"link = { ck3 = ck3CB1 ir = goal1 }" +
			"link = { ck3 = ck3CB2 ir = goal2 }"
		);
		var mapper = new WarMapper(tempTestFile);

		var ck3CB = mapper.GetCK3CBForImperatorWarGoal("goal2");
		Assert.Equal("ck3CB2", ck3CB);
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