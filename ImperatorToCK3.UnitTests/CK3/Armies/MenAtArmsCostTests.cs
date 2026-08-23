using commonItems;
using ImperatorToCK3.CK3.Armies;
using Xunit;

namespace ImperatorToCK3.UnitTests.CK3.Armies;

public class MenAtArmsCostTests {
	[Fact]
	public void ElementsDefaultToNull() {
		var cost = new MenAtArmsCost();
		Assert.Null(cost.Gold);
		Assert.Null(cost.Piety);
		Assert.Null(cost.Prestige);
	}

	[Fact]
	public void ElementsCanBeRead() {
		var reader = new BufferedReader("{ gold=1 piety=-2 prestige=5.49}");
		var cost = new MenAtArmsCost(reader, new ScriptValueCollection());

		Assert.Equal(1, cost.Gold);
		Assert.Equal(-2, cost.Piety);
		Assert.Equal(5.49, cost.Prestige);
	}

	[Fact]
	public void CostCanBeDivided() {
		var cost = new MenAtArmsCost { Gold = 1, Piety = -2, Prestige = 2.5 };
		var dividedCost = cost / 2;
		Assert.Equal(0.5, dividedCost.Gold);
		Assert.Equal(-1, dividedCost.Piety);
		Assert.Equal(1.25, dividedCost.Prestige);
	}

	[Fact]
	public void CostCanBeMultiplied() {
		var cost = new MenAtArmsCost { Gold = 1, Piety = -2, Prestige = 2.5 };
		var dividedCost = cost * 2;
		Assert.Equal(2, dividedCost.Gold);
		Assert.Equal(-4, dividedCost.Piety);
		Assert.Equal(5, dividedCost.Prestige);
	}

	[Fact]
	public void NullElementsRemainNullWhenCostIsDivided() {
		var cost = new MenAtArmsCost();
		var dividedCost = cost / 2;

		Assert.NotNull(dividedCost);
		Assert.Null(dividedCost.Gold);
		Assert.Null(dividedCost.Piety);
		Assert.Null(dividedCost.Prestige);
	}

	[Fact]
	public void NullElementsRemainNullWhenCostIsMultiplied() {
		var cost = new MenAtArmsCost();
		var multipliedCost = cost * 2;

		Assert.NotNull(multipliedCost);
		Assert.Null(multipliedCost.Gold);
		Assert.Null(multipliedCost.Piety);
		Assert.Null(multipliedCost.Prestige);
	}

	[Fact]
	public void DividedCostKeepsNullElementsAndDividesTheRest() {
		var cost = new MenAtArmsCost { Gold = 1, Piety = null, Prestige = 2.5 };
		var dividedCost = cost / 2;

		Assert.Equal(0.5, dividedCost.Gold);
		Assert.Null(dividedCost.Piety);
		Assert.Equal(1.25, dividedCost.Prestige);
	}

	[Fact]
	public void MultipliedCostKeepsNullElementsAndMultipliesTheRest() {
		var cost = new MenAtArmsCost { Gold = null, Piety = -2, Prestige = null };
		var multipliedCost = cost * 2;

		Assert.Null(multipliedCost.Gold);
		Assert.Equal(-4, multipliedCost.Piety);
		Assert.Null(multipliedCost.Prestige);
	}
}