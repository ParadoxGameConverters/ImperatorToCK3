using commonItems;
using ImperatorToCK3.Mappers.Government;
using System.Collections.Generic;
using Xunit;

namespace ImperatorToCK3.UnitTests.Mappers.Government;

public class GovernmentMapperTests {
	[Fact]
	public void NonMatchGivesNull() {
		var reader = new BufferedReader("link = { ck3 = ck3Government ir = irGovernment }");
		var mapper = new GovernmentMapper(reader, ck3GovernmentIds: new List<string> { "ck3Government" });
		var ck3Gov = mapper.GetCK3GovernmentForImperatorGovernment("nonMatchingGovernment", null);
		Assert.Null(ck3Gov);
	}
	[Fact]
	public void CK3GovernmentCanBeFound() {
		var reader = new BufferedReader("link = { ck3 = ck3Government ir = irGovernment }");
		var mapper = new GovernmentMapper(reader, ck3GovernmentIds: new List<string> { "ck3Government" });
		var ck3Gov = mapper.GetCK3GovernmentForImperatorGovernment("irGovernment", null);
		Assert.Equal("ck3Government", ck3Gov);
	}
	[Fact]
	public void MultipleImperatorGovernmentsCanBeInARule() {
		var reader = new BufferedReader("link = { ck3 = ck3Government ir = irGovernment ir = irGovernment2 }");
		var mapper = new GovernmentMapper(reader, ck3GovernmentIds: new List<string> { "ck3Government" });
		var ck3Gov1 = mapper.GetCK3GovernmentForImperatorGovernment("irGovernment", null);
		var ck3Gov2 = mapper.GetCK3GovernmentForImperatorGovernment("irGovernment2", null);
		Assert.Equal("ck3Government", ck3Gov1);
		Assert.Equal("ck3Government", ck3Gov2);
	}
	[Fact]
	public void CorrectRuleMatches() {
		var reader = new BufferedReader(
			"link = { ck3 = ck3Government ir = irGovernment }\n" +
			"link = { ck3 = ck3Government2 ir = irGovernment2 }"
		);
		var mapper = new GovernmentMapper(reader, ck3GovernmentIds: new List<string> { "ck3Government", "ck3Government2" });
		var ck3Gov = mapper.GetCK3GovernmentForImperatorGovernment("irGovernment2", null);
		Assert.Equal("ck3Government2", ck3Gov);
	}

	[Fact]
	public void CultureCanBeUsedToMatch() {
		var reader = new BufferedReader(
			"link = { ck3 = govA ir = irGovernment irCulture = roman }\n" +
			"link = { ck3 = govB ir = irGovernment irCulture = greek }\n" +
			"link = { ck3 = govC ir = irGovernment }"
		);
		var mapper = new GovernmentMapper(reader, ck3GovernmentIds: new List<string> { "govA", "govB", "govC" });
		Assert.Equal("govA", mapper.GetCK3GovernmentForImperatorGovernment("irGovernment", "roman"));
		Assert.Equal("govB", mapper.GetCK3GovernmentForImperatorGovernment("irGovernment", "greek"));
		Assert.Equal("govC", mapper.GetCK3GovernmentForImperatorGovernment("irGovernment", "thracian"));
		Assert.Equal("govC", mapper.GetCK3GovernmentForImperatorGovernment("irGovernment", null));
	}
}