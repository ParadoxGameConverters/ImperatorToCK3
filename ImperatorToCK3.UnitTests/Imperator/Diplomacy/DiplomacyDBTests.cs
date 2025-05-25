using commonItems;
using System;
using System.IO;
using Xunit;

namespace ImperatorToCK3.UnitTests.Imperator.Diplomacy;

[Collection("Sequential")]
[CollectionDefinition("Sequential", DisableParallelization = true)]
public class DiplomacyDBTests {
	[Fact]
	public void WarWithNoAttackersIsSkipped() {
		var output = new StringWriter();
		Console.SetOut(output);

		var reader = new BufferedReader("""
			database = {
				1 = { previous=no }
				2 = { previous=no defender=1 }
			}
		""");
		var diplomacy = new ImperatorToCK3.Imperator.Diplomacy.DiplomacyDB(reader);

		Assert.Empty(diplomacy.Wars);
		var logStr = output.ToString();
		Assert.Contains("[DEBUG] Skipping war 1 has no attackers!", logStr);
		Assert.Contains("[DEBUG] Skipping war 2 has no attackers!", logStr);
	}
	
	[Fact]
	public void WarWithNoDefendersIsSkipped() {
		var output = new StringWriter();
		Console.SetOut(output);

		var reader = new BufferedReader("""
            database = {
                1 = { previous=no attacker=1 }
            }
        """);
		var diplomacy = new ImperatorToCK3.Imperator.Diplomacy.DiplomacyDB(reader);

		Assert.Empty(diplomacy.Wars);
		var logStr = output.ToString();
		Assert.Contains("[DEBUG] Skipping war 1 has no defenders!", logStr);
	}
	
	[Fact]
	public void WarWithNoWarGoalIsSkipped() {
		var output = new StringWriter();
		Console.SetOut(output);

		var reader = new BufferedReader("""
			database = {
				1 = { previous=no attacker=1 defender=2 }
			}
		""");
		var diplomacy = new ImperatorToCK3.Imperator.Diplomacy.DiplomacyDB(reader);

		Assert.Empty(diplomacy.Wars);
		var logStr = output.ToString();
		Assert.Contains("[WARN] Skipping war 1 with no wargoal!", logStr);
	}
	
	[Fact]
	public void PreviousWarsAreSkipped() {
		var reader = new BufferedReader("database = { 1 = { previous=yes } }");
		var diplomacy = new ImperatorToCK3.Imperator.Diplomacy.DiplomacyDB(reader);

		Assert.Empty(diplomacy.Wars);
	}

	[Fact]
	public void WarCanBeLoaded() {
		var reader = new BufferedReader("""
        	database = {
        		1 = {
        			attacker=1 defender=2 start_date=1.1.1 take_province={ type="conquer_wargoal" state=3 }
        		}
        	}
        """);
		var diplomacy = new ImperatorToCK3.Imperator.Diplomacy.DiplomacyDB(reader);
		
		Assert.Single(diplomacy.Wars);
		Assert.Equal(new Date("1.1.1", AUC: true), diplomacy.Wars[0].StartDate);
		Assert.Equal((ulong)1, diplomacy.Wars[0].AttackerCountryIds[0]);
		Assert.Equal((ulong)2, diplomacy.Wars[0].DefenderCountryIds[0]);
		Assert.Equal("conquer_wargoal", diplomacy.Wars[0].WarGoal);
		Assert.Equal((ulong)3, diplomacy.Wars[0].TargetedStateId);
	}

	[Fact]
	public void DependencyCanBeLoaded() {
		var reader = new BufferedReader("dependency = { first=1 second=2 start_date=1.1.1 subject_type=tributary }");
		var diplomacy = new ImperatorToCK3.Imperator.Diplomacy.DiplomacyDB(reader);
		
		Assert.Single(diplomacy.Dependencies);
		Assert.Equal((ulong)1, diplomacy.Dependencies[0].OverlordId);
		Assert.Equal((ulong)2, diplomacy.Dependencies[0].SubjectId);
		Assert.Equal(new Date("1.1.1", AUC: true), diplomacy.Dependencies[0].StartDate);
		Assert.Equal("tributary", diplomacy.Dependencies[0].SubjectType);
	}

	[Fact]
	public void DefensiveLeagueCanBeLoaded() {
		var reader = new BufferedReader(
			"""
			defensive_league={
				member=7
				member=552
			}
			""");
		var diplomacy = new ImperatorToCK3.Imperator.Diplomacy.DiplomacyDB(reader);

		Assert.Single(diplomacy.DefensiveLeagues);
		Assert.Equal(2, diplomacy.DefensiveLeagues[0].Count);
		Assert.Equal((ulong)7, diplomacy.DefensiveLeagues[0][0]);
		Assert.Equal((ulong)552, diplomacy.DefensiveLeagues[0][1]);
	}
}