using commonItems;
using System;
using System.IO;
using Xunit;

namespace ImperatorToCK3.UnitTests.Imperator.Diplomacy; 

[Collection("Sequential")]
[CollectionDefinition("Sequential", DisableParallelization = true)]
public class DiplomacyTests {
	[Fact]
	public void WarWithNoDefendersIsSkipped() {
		var output = new StringWriter();
		Console.SetOut(output);
		
		var reader = new BufferedReader("""
			database = {
				1 = { previous=no }
				2 = { previous=no attacker=1 }
				3 = { previous=no defender=1 }
			}
		""");
		var diplomacy = new ImperatorToCK3.Imperator.Diplomacy.Diplomacy(reader);
		
		Assert.Empty(diplomacy.Wars);
		var logStr = output.ToString();
		Assert.Contains("[DEBUG] Skipping war 1 has no attackers!", logStr);
		Assert.Contains("[DEBUG] Skipping war 2 has no defenders!", logStr);
		Assert.Contains("[DEBUG] Skipping war 3 has no attackers!", logStr);
	}
}