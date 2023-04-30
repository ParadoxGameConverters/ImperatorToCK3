using ImperatorToCK3.Imperator.Diplomacy;
using commonItems;
using Xunit;

namespace ImperatorToCK3.UnitTests.Imperator.Diplomacy;

public class WarTests {
	[Fact]
	public void FieldsCanBeSet() {
		var reader = new BufferedReader("""
			attacker = 1
			attacker = 11
			defender = 2
			defender = 22
			start_date = 10.11.12
			naval_superiority = { type = naval_wargoal }
			unusedToken = unusedValue
		""");
		var war = War.Parse(reader);
		Assert.Equal(new Date(10, 11, 12, AUC: true), war.StartDate);
		Assert.Collection(war.AttackerCountryIds,
			attacker1 => Assert.Equal((ulong)1, attacker1),
			attacker2 => Assert.Equal((ulong)11, attacker2)
		);
		Assert.Collection(war.DefenderCountryIds,
			defender1 => Assert.Equal((ulong)2, defender1),
			defender2 => Assert.Equal((ulong)22, defender2)
		);
		Assert.Equal("naval_wargoal", war.WarGoal);
	}
}