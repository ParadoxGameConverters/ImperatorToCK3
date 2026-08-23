using commonItems;
using DotLiquid;
using ImperatorToCK3.Mappers.SuccessionLaw;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace ImperatorToCK3.UnitTests.Mappers.SuccessionLaw;

[Collection("Sequential")]
[CollectionDefinition("Sequential", DisableParallelization = true)]
public class SuccessionLawMapperTests {
	private static readonly string[] enabledCK3Dlcs = [];
	private static readonly Hash liquidVariables = new();
	
	[Fact]
	public void NonMatchGivesEmptySet() {
		var reader = new BufferedReader("link = { ir=implaw ck3 = ck3law }");
		var mapper = new SuccessionLawMapper(reader);

		var ck3Laws = mapper.GetCK3LawsForImperatorLaws(impLaws: ["madeUpLaw"], irGovernment: null, enabledCK3Dlcs);
		Assert.Empty(ck3Laws);
	}

	[Fact]
	public void CK3LawCanBeFound() {
		var reader = new BufferedReader("link = { ir=implaw ck3 = ck3law }");
		var mapper = new SuccessionLawMapper(reader);

		var ck3Laws = mapper.GetCK3LawsForImperatorLaws(impLaws: ["implaw"], irGovernment: null, enabledCK3Dlcs);
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

		var ck3Laws = mapper.GetCK3LawsForImperatorLaws(impLaws: ["implaw", "implaw3"], irGovernment: null, enabledCK3Dlcs);
		var expectedReturnedLaws = new SortedSet<string> { "ck3law", "ck3law2", "ck3law5" };
		Assert.Equal(expectedReturnedLaws, ck3Laws);
	}

	[Fact]
	public void EnabledCK3DlcsCanBeUsedInMappings() {
		var reader = new BufferedReader(
			"""
			link = { ir=implaw ck3=ck3lawForDLC has_ck3_dlc=roads_to_power }
			link = { ir=implaw ck3=ck3law }
			"""
		);
		var mapper = new SuccessionLawMapper(reader);
		
		var ck3LawsWithDlc = mapper.GetCK3LawsForImperatorLaws(
			impLaws: ["implaw"],
			irGovernment: null,
			enabledCK3Dlcs:["roads_to_power"]);
		Assert.Equal(["ck3lawForDLC"], ck3LawsWithDlc);

		var ck3LawsWithoutDlc = mapper.GetCK3LawsForImperatorLaws(
			impLaws: ["implaw"],
			irGovernment: null,
			enabledCK3Dlcs: []);
		Assert.Equal(["ck3law"], ck3LawsWithoutDlc);
	}

	[Fact]
	public void ImperatorGovernmentCanBeUsedInMappings() {
		var reader = new BufferedReader(
			"""
			link = { ir=implaw ck3=ck3law1 ir_government=imperium ir_government=imperial_cult }
			link = { ir=implaw ck3=ck3law2 }
			"""
		);
		var mapper = new SuccessionLawMapper(reader);
		
		var ck3LawsWithImperialGov = mapper.GetCK3LawsForImperatorLaws(
			impLaws: ["implaw"],
			irGovernment: "imperium",
			enabledCK3Dlcs: enabledCK3Dlcs);
		Assert.Equal(["ck3law1"], ck3LawsWithImperialGov);
		
		var ck3LawsWithoutImperialGov = mapper.GetCK3LawsForImperatorLaws(
			impLaws: ["implaw"],
			irGovernment: "imperial_cult",
			enabledCK3Dlcs: enabledCK3Dlcs);
		Assert.Equal(["ck3law1"], ck3LawsWithoutImperialGov);
		
		var ck3LawsWithoutImperatorGov = mapper.GetCK3LawsForImperatorLaws(
			impLaws: ["implaw"],
			irGovernment: "madeUpGov",
			enabledCK3Dlcs: enabledCK3Dlcs);
		Assert.Equal(["ck3law2"], ck3LawsWithoutImperatorGov);
	}

	[Fact]
	public void MappingsAreReadFromFile() {
		var mapper = new SuccessionLawMapper("TestFiles/configurables/succession_law_map.liquid", liquidVariables);
		Assert.Equal(
			["ck3law1", "ck3law2"],
			mapper.GetCK3LawsForImperatorLaws(impLaws: ["implaw1"], irGovernment: null, enabledCK3Dlcs)
		);
		Assert.Equal(
			["ck3law3"],
			mapper.GetCK3LawsForImperatorLaws(impLaws: ["implaw2"], irGovernment: null, enabledCK3Dlcs)
		);
	}
}