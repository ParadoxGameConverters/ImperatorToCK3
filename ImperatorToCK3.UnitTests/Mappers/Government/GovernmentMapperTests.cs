using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.Mappers.Government;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace ImperatorToCK3.UnitTests.Mappers.Government;

[Collection("Sequential")]
[CollectionDefinition("Sequential", DisableParallelization = true)]
public class GovernmentMapperTests {
	[Fact]
	public void NonMatchGivesNull() {
		Directory.CreateDirectory("configurables");
		File.WriteAllText("configurables/government_map.txt", "link = { ck3 = ck3Government ir = irGovernment }");
		var mapper = new GovernmentMapper(ck3GovernmentIds: ["ck3Government"]);
		File.Delete("configurables/government_map.txt"); // cleanup
		
		var ck3Gov = mapper.GetCK3GovernmentForImperatorGovernment("nonMatchingGovernment", rank: null, irCultureId: null, []);
		Assert.Null(ck3Gov);
	}
	[Fact]
	public void CK3GovernmentCanBeFound() {
		Directory.CreateDirectory("configurables");
		File.WriteAllText("configurables/government_map.txt", "link = { ck3 = ck3Government ir = irGovernment }");
		var mapper = new GovernmentMapper(ck3GovernmentIds: ["ck3Government"]);
		File.Delete("configurables/government_map.txt"); // cleanup
		
		var ck3Gov = mapper.GetCK3GovernmentForImperatorGovernment("irGovernment", rank: null, irCultureId: null, []);
		Assert.Equal("ck3Government", ck3Gov);
	}
	[Fact]
	public void MultipleImperatorGovernmentsCanBeInARule() {
		Directory.CreateDirectory("configurables");
		File.WriteAllText("configurables/government_map.txt", "link = { ck3 = ck3Government ir = irGovernment ir = irGovernment2 }");
		var mapper = new GovernmentMapper(ck3GovernmentIds: ["ck3Government"]);
		File.Delete("configurables/government_map.txt"); // cleanup
		
		var ck3Gov1 = mapper.GetCK3GovernmentForImperatorGovernment("irGovernment", rank: null, irCultureId: null, []);
		var ck3Gov2 = mapper.GetCK3GovernmentForImperatorGovernment("irGovernment2", rank: null, irCultureId: null, []);
		Assert.Equal("ck3Government", ck3Gov1);
		Assert.Equal("ck3Government", ck3Gov2);
	}
	[Fact]
	public void CorrectRuleMatches() {
		Directory.CreateDirectory("configurables");
		File.WriteAllText("configurables/government_map.txt",
			"link = { ck3 = ck3Government ir = irGovernment }\n" +
			"link = { ck3 = ck3Government2 ir = irGovernment2 }"
		);
		var mapper = new GovernmentMapper(ck3GovernmentIds: ["ck3Government", "ck3Government2"]);
		File.Delete("configurables/government_map.txt"); // cleanup
		
		var ck3Gov = mapper.GetCK3GovernmentForImperatorGovernment("irGovernment2", rank: null, irCultureId: null, []);
		Assert.Equal("ck3Government2", ck3Gov);
	}

	[Fact]
	public void CultureCanBeUsedToMatch() {
		Directory.CreateDirectory("configurables");
		File.WriteAllText("configurables/government_map.txt", 
			"link = { ck3 = govA ir = irGovernment irCulture = roman }\n" +
			"link = { ck3 = govB ir = irGovernment irCulture = greek }\n" +
			"link = { ck3 = govC ir = irGovernment }"
		);
		var mapper = new GovernmentMapper(ck3GovernmentIds: ["govA", "govB", "govC"]);
		File.Delete("configurables/government_map.txt"); // cleanup
		
		Assert.Equal("govA", mapper.GetCK3GovernmentForImperatorGovernment("irGovernment", rank: null, "roman", []));
		Assert.Equal("govB", mapper.GetCK3GovernmentForImperatorGovernment("irGovernment", rank: null, "greek", []));
		Assert.Equal("govC", mapper.GetCK3GovernmentForImperatorGovernment("irGovernment", rank: null, "thracian", []));
		Assert.Equal("govC", mapper.GetCK3GovernmentForImperatorGovernment("irGovernment", rank: null, irCultureId: null, []));
	}

	[Fact]
	public void CK3TitleRankCanBeUsedToMatch() {
		Directory.CreateDirectory("configurables");
		File.WriteAllText("configurables/government_map.txt", 
			"""
			link = { ck3 = administrative_government ir = imperium ck3_title_rank = ke } # only for kingdoms and empires
			link = { ck3 = feudal_government ir = imperium }
			"""
		);
		var mapper = new GovernmentMapper(ck3GovernmentIds: [ "administrative_government", "feudal_government" ]);
		File.Delete("configurables/government_map.txt"); // cleanup

		foreach (var rank in new List<TitleRank> { TitleRank.empire, TitleRank.kingdom }) {
			Assert.Equal("administrative_government",
				mapper.GetCK3GovernmentForImperatorGovernment(
					irGovernmentId: "imperium", rank, irCultureId: null, enabledCK3Dlcs: []));
		}
		foreach (var rank in new List<TitleRank> { TitleRank.duchy, TitleRank.county, TitleRank.barony }) {
			Assert.Equal("feudal_government",
				mapper.GetCK3GovernmentForImperatorGovernment(
					irGovernmentId: "imperium", rank, irCultureId: null, enabledCK3Dlcs: []));
		}
	}

	[Fact]
	public void CK3DlcCanBeUsedToMatch() {
		Directory.CreateDirectory("configurables");
		File.WriteAllText("configurables/government_map.txt",
			"""
			link = { ck3 = administrative_government
				has_ck3_dlc = roads_to_power
				ir = imperium
				ir = imperial_cult
			}
			link = { ck3 = feudal_government
				ir = imperium # When the user doesn't have the Roads to Power DLC.
				ir = imperial_cult # When the user doesn't have the Roads to Power DLC.
			}
			""");
		var mapper = new GovernmentMapper(ck3GovernmentIds: [ "administrative_government", "feudal_government" ]);
		File.Delete("configurables/government_map.txt"); // cleanup
			
		Assert.Equal("administrative_government", mapper.GetCK3GovernmentForImperatorGovernment("imperium", rank: null, irCultureId: null, enabledCK3Dlcs: ["roads_to_power"]));
		Assert.Equal("feudal_government", mapper.GetCK3GovernmentForImperatorGovernment("imperium", rank: null, irCultureId: null, enabledCK3Dlcs: []));
	}
}