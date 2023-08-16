using commonItems;
using ImperatorToCK3.CK3.Armies;
using Xunit;

namespace ImperatorToCK3.UnitTests.CK3.Armies; 

public class MenAtArmsTypeTests {
	[Fact]
	public void MenAtArmsTypeIsCorrectlySerialized() {
		var maaTypeReader = new BufferedReader("""
		{
			type = pikemen
			
			damage = 30
			toughness = 24
			
			terrain_bonus = {
				mountains = { damage = 5 toughness = 12 }
				desert_mountains = { damage = 5 toughness = 12 }
				hills = { damage = 3 toughness = 8 }
			}

			counters = {
				pikemen = 0.5
				light_cavalry = 2
				heavy_cavalry = 2
			}

			buy_cost = { gold = landsknecht_recruitment_cost }
			low_maintenance_cost = { gold = landsknecht_low_maint_cost }
			high_maintenance_cost = { gold = landsknecht_high_maint_cost }
			
			stack = 100
			ai_quality = { value = @cultural_maa_extra_ai_score }
			icon = pikemen
		}
		""");
		
		var menAtArmsType = new MenAtArmsType("landsknecht", maaTypeReader, new ScriptValueCollection());
		var serializedType = menAtArmsType.Serialize(indent: string.Empty, withBraces: true);

		Assert.Contains("damage = 30", serializedType);
		Assert.Contains("toughness = 24", serializedType);
		Assert.Contains("terrain_bonus = {", serializedType);
		Assert.Contains("buy_cost={", serializedType);
		Assert.Contains("stack = 100", serializedType);
		Assert.Contains("icon = pikemen", serializedType);
	}
}