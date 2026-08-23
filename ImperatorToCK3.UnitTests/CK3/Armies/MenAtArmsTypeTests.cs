using commonItems;
using commonItems.Collections;
using ImperatorToCK3.CK3.Armies;
using ImperatorToCK3.CK3.Characters;
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
		Assert.Contains("buy_cost = {", serializedType);
		Assert.Contains("stack = 100", serializedType);
		Assert.Contains("icon = pikemen", serializedType);
	}

	[Fact]
	public void MenAtArmsTypeHasCorrectDefaults() {
		var maaTypeReader = new BufferedReader("{}");
		var menAtArmsType = new MenAtArmsType("empty_type", maaTypeReader, new ScriptValueCollection());

		Assert.Equal("empty_type", menAtArmsType.Id);
		Assert.Equal("{}", menAtArmsType.CanRecruit.ToString());
		Assert.Equal(100, menAtArmsType.Stack);
		Assert.Null(menAtArmsType.BuyCost);
		Assert.Null(menAtArmsType.LowMaintenanceCost);
		Assert.Null(menAtArmsType.HighMaintenanceCost);
		Assert.Null(menAtArmsType.ProvisionCost);
		Assert.DoesNotContain("damage =", menAtArmsType.Serialize(indent: string.Empty, withBraces: true));
		Assert.False(menAtArmsType.ToBeOutputted);
	}

	[Fact]
	public void MenAtArmsTypeParsesCanRecruitProvisionCostAndCosts() {
		var maaTypeReader = new BufferedReader("""
		{
			can_recruit = culture
			stack = 150
			buy_cost = { gold = 10 piety = 1 prestige = 2 }
			low_maintenance_cost = { gold = 0.1 piety = 0.2 prestige = 0.3 }
			high_maintenance_cost = { gold = 0.4 piety = 0.5 prestige = 0.6 }
			provision_cost = 1.5
			damage = 12
		}
		""");

		var menAtArmsType = new MenAtArmsType("archers", maaTypeReader, new ScriptValueCollection());

		Assert.Equal("culture", menAtArmsType.CanRecruit.ToString());
		Assert.Equal(150, menAtArmsType.Stack);

		Assert.NotNull(menAtArmsType.BuyCost);
		Assert.Equal(10, menAtArmsType.BuyCost.Gold);
		Assert.Equal(1, menAtArmsType.BuyCost.Piety);
		Assert.Equal(2, menAtArmsType.BuyCost.Prestige);

		Assert.NotNull(menAtArmsType.LowMaintenanceCost);
		Assert.Equal(0.1, menAtArmsType.LowMaintenanceCost.Gold);
		Assert.NotNull(menAtArmsType.HighMaintenanceCost);
		Assert.Equal(0.4, menAtArmsType.HighMaintenanceCost.Gold);

		Assert.Equal(1.5, menAtArmsType.ProvisionCost);

		Assert.Contains("damage = 12", menAtArmsType.Serialize(indent: string.Empty, withBraces: true));
	}

	[Fact]
	public void DerivedMenAtArmsTypeScalesCostsAndOverridesAttributes() {
		var baseReader = new BufferedReader("""
		{
			stack = 100
			damage = 10
			toughness = 5
			can_recruit = culture
			buy_cost = { gold = 10 }
			low_maintenance_cost = { gold = 1 piety = 0.5 prestige = 0.2 }
			high_maintenance_cost = { gold = 2 piety = 0.6 prestige = 0.4 }
			provision_cost = 2.5
		}
		""");
		var baseType = new MenAtArmsType("archers", baseReader, new ScriptValueCollection());

		var characters = new CharacterCollection();
		var character = new Character("42", "Testovirus", new Date(800, 1, 1), characters);
		characters.Add(character);

		var bookmarkDate = new Date(867, 1, 1);
		var derivedType = new MenAtArmsType(baseType, character, stack: 200, bookmarkDate: bookmarkDate);

		Assert.True(derivedType.ToBeOutputted);
		Assert.Equal($"IRToCK3_maa_{character.Id}_{baseType.Id}", derivedType.Id);
		Assert.Equal(200, derivedType.Stack);

		var canRecruit = derivedType.CanRecruit.ToString();
		Assert.Contains($"exists=character:{character.Id}", canRecruit);
		Assert.Contains($"this=character:{character.Id}", canRecruit);
		Assert.Contains("current_date<=867.2.1", canRecruit);

		Assert.NotNull(derivedType.BuyCost);
		Assert.Equal(0, derivedType.BuyCost.Gold);

		var stackRatio = 200 / 100;
		Assert.NotNull(derivedType.LowMaintenanceCost);
		Assert.Equal(baseType.LowMaintenanceCost!.Gold * stackRatio, derivedType.LowMaintenanceCost.Gold);
		Assert.Equal(baseType.LowMaintenanceCost.Piety * stackRatio, derivedType.LowMaintenanceCost.Piety);
		Assert.Equal(baseType.LowMaintenanceCost.Prestige * stackRatio, derivedType.LowMaintenanceCost.Prestige);

		Assert.NotNull(derivedType.HighMaintenanceCost);
		Assert.Equal(baseType.HighMaintenanceCost!.Gold * stackRatio, derivedType.HighMaintenanceCost.Gold);
		Assert.Equal(baseType.HighMaintenanceCost.Piety * stackRatio, derivedType.HighMaintenanceCost.Piety);
		Assert.Equal(baseType.HighMaintenanceCost.Prestige * stackRatio, derivedType.HighMaintenanceCost.Prestige);

		Assert.NotNull(derivedType.ProvisionCost);
		Assert.Equal(baseType.ProvisionCost!.Value * stackRatio, derivedType.ProvisionCost.Value);

		var serializedDerivedType = derivedType.Serialize(indent: string.Empty, withBraces: true);
		Assert.Contains("damage = 10", serializedDerivedType);
		Assert.Contains("ai_quality = { value=1 }", serializedDerivedType);
	}

	[Fact]
	public void DerivedMenAtArmsTypeSkipsUnsetBaseCosts() {
		var baseReader = new BufferedReader("""
		{
			stack = 50
			icon = light_footmen
		}
		""");
		var baseType = new MenAtArmsType("light_footmen", baseReader, new ScriptValueCollection());

		var characters = new CharacterCollection();
		var character = new Character("7", "Seven", new Date(800, 1, 1), characters);
		characters.Add(character);

		var derivedType = new MenAtArmsType(baseType, character, stack: 25, bookmarkDate: new Date(1066, 12, 25));

		Assert.Null(derivedType.BuyCost?.Piety);
		Assert.Null(derivedType.BuyCost?.Prestige);
		Assert.Null(derivedType.LowMaintenanceCost);
		Assert.Null(derivedType.HighMaintenanceCost);
		Assert.Null(derivedType.ProvisionCost);
		Assert.Contains("icon = light_footmen", derivedType.Serialize(indent: string.Empty, withBraces: true));
	}
}