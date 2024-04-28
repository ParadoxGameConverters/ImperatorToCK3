using commonItems;
using ImperatorToCK3.Mappers.Technology;
using System;
using System.IO;
using Xunit;

namespace ImperatorToCK3.UnitTests.Mappers.Technology;

[Collection("Sequential")]
[CollectionDefinition("Sequential", DisableParallelization = true)]
public class InnovationLinkTests {
	[Fact]
	public void WarningIsLoggedWhenLinkHasNoCK3Innovation() {
		var output = new StringWriter();
		Console.SetOut(output);
		
		var reader = new BufferedReader("ir = ir_invention");
		var link = new InnovationLink(reader);
		Assert.Null(link.CK3InnovationId);
		
		Assert.Contains("[WARN] Innovation link from ir_invention has no CK3 innovation.", output.ToString());
	}
	
	[Fact]
	public void NullIsReturnedForNonMatchingInvention() {
		var reader = new BufferedReader("ir = ir_invention\nck3 = ck3_innovation");
		var link = new InnovationLink(reader);
		
		Assert.Null(link.Match("non_matching_invention"));
	}
	
	[Fact]
	public void CK3InnovationIdIsReturnedForMatchingInvention() {
		var reader = new BufferedReader("ir = ir_invention\nck3 = ck3_innovation");
		var link = new InnovationLink(reader);
		
		Assert.Equal("ck3_innovation", link.Match("ir_invention"));
	}
}