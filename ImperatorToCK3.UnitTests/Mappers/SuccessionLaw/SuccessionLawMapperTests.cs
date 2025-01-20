using commonItems;
using ImperatorToCK3.Mappers.SuccessionLaw;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace ImperatorToCK3.UnitTests.Mappers.SuccessionLaw;

[Collection("Sequential")]
[CollectionDefinition("Sequential", DisableParallelization = true)]
public class SuccessionLawMapperTests {
	private static readonly OrderedDictionary<string, bool> ck3ModFlags = [];
	private static readonly string[] enabledCK3Dlcs = [];
	
	[Fact]
	public void NonMatchGivesEmptySet() {
		var reader = new BufferedReader("link = { ir=implaw ck3 = ck3law }");
		var mapper = new SuccessionLawMapper(reader);

		var ck3Laws = mapper.GetCK3LawsForImperatorLaws(["madeUpLaw"], enabledCK3Dlcs);
		Assert.Empty(ck3Laws);
	}

	[Fact]
	public void CK3LawCanBeFound() {
		var reader = new BufferedReader("link = { ir=implaw ck3 = ck3law }");
		var mapper = new SuccessionLawMapper(reader);

		var ck3Laws = mapper.GetCK3LawsForImperatorLaws(["implaw"], enabledCK3Dlcs);
		Assert.Equal(["ck3law"], ck3Laws);
	}

	[Fact]
	public void LinkWithNoCK3LawResultsInWarning() {
		var output = new StringWriter();
		Console.SetOut(output);

		var reader = new BufferedReader("link = { ir=implaw }");
		_ = new SuccessionLawMapper(reader);

		Assert.Contains("SuccessionLawMapper: link with no CK3 successions laws", output.ToString());
	}

	[Fact]
	public void MultipleLawsCanBeReturned() {
		var reader = new BufferedReader(
			"""
			link = { ir=implaw ck3 = ck3law ck3 = ck3law2 }
			link = { ir=implaw ck3 = ck3law3 } # Will not be used because the first link matches implaw
			link = { ir=implaw2 ck3 = ck3law4 }
			link = { ir=implaw3 ck3 = ck3law5 }
			"""
		);
		var mapper = new SuccessionLawMapper(reader);

		var ck3Laws = mapper.GetCK3LawsForImperatorLaws(["implaw", "implaw3"], enabledCK3Dlcs);
		var expectedReturnedLaws = new SortedSet<string> { "ck3law", "ck3law2", "ck3law5" };
		Assert.Equal(expectedReturnedLaws, ck3Laws);
	}

	[Fact]
	public void EnabledDlcsCanBeUsedInMappings() {
		var reader = new BufferedReader(
			"""
			link = { ir=implaw ck3=ck3lawForDLC has_ck3_dlc=roads_to_power }
			link = { ir=implaw ck3=ck3law }
			"""
		);
		var mapper = new SuccessionLawMapper(reader);
		
		var ck3LawsWithDlc = mapper.GetCK3LawsForImperatorLaws(["implaw"], ["roads_to_power"]);
		Assert.Equal(["ck3lawForDLC"], ck3LawsWithDlc);
		
		var ck3LawsWithoutDlc = mapper.GetCK3LawsForImperatorLaws(["implaw"], []);
		Assert.Equal(["ck3law"], ck3LawsWithoutDlc);
	}

	[Fact]
	public void MappingsAreReadFromFile() {
		var mapper = new SuccessionLawMapper("TestFiles/configurables/succession_law_map.txt", ck3ModFlags);
		Assert.Equal(
			["ck3law1", "ck3law2"],
			mapper.GetCK3LawsForImperatorLaws(["implaw1"], enabledCK3Dlcs)
		);
		Assert.Equal(
			["ck3law3"],
			mapper.GetCK3LawsForImperatorLaws(["implaw2"], enabledCK3Dlcs)
		);
	}
}