using commonItems;
using ImperatorToCK3.Mappers.Technology;
using System;
using System.IO;
using Xunit;

namespace ImperatorToCK3.UnitTests.Mappers.Technology;

[Collection("Sequential")]
[CollectionDefinition("Sequential", DisableParallelization = true)]
public class InnovationBonusTests {
	[Fact]
	public void WarningIsLoggedWhenBonusHasNoCK3Innovation() {
		var output = new StringWriter();
		Console.SetOut(output);
		
		var reader = new BufferedReader("ir = ir_invention");
		var bonus = new InnovationBonus(reader);
		Assert.Null(bonus.CK3InnovationId);
		
		Assert.Contains("[WARN] Innovation bonus from ir_invention has no CK3 innovation.", output.ToString());
	}
	
	[Fact]
	public void WarningIsLoggedWhenBonusHasNoImperatorInvention() {
		var output = new StringWriter();
		Console.SetOut(output);
		
		var reader = new BufferedReader("ck3 = ck3_innovation");
		_ = new InnovationBonus(reader);
		
		Assert.Contains("[WARN] Innovation bonus to ck3_innovation has no Imperator invention.", output.ToString());
	}

	[Fact]
	public void GetProgressReturnsNullForNoProgress() {
		var reader = new BufferedReader("ir = ir_invention\nck3 = ck3_innovation");
		var bonus = new InnovationBonus(reader);
		
		Assert.Null(bonus.GetProgress(Array.Empty<string>()));
		Assert.Null(bonus.GetProgress(["ir_other_invention"]));
	}

	[Fact]
	public void EveryMatchingInventionGives25Progress() {
		var reader = new BufferedReader("ir=inv1 ir=inv2 ir=inv3 ir=inv4 ck3=ck3_innovation");
		var bonus = new InnovationBonus(reader);

		Assert.Equal("ck3_innovation", bonus.CK3InnovationId);
		Assert.Equal(new("ck3_innovation", 25), bonus.GetProgress(["inv1"])!.Value);
		Assert.Equal(new("ck3_innovation", 50), bonus.GetProgress(["inv1", "inv2"])!.Value);
		Assert.Equal(new("ck3_innovation", 75), bonus.GetProgress(["inv1", "inv2", "inv3"])!.Value);
		Assert.Equal(new("ck3_innovation", 100), bonus.GetProgress(["inv1", "inv2", "inv3", "inv4"])!.Value);
	}
}