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
	[Fact]
	public void NonMatchGivesEmptySet() {
		var reader = new BufferedReader("link = { imp = implaw ck3 = ck3law }");
		var mapper = new SuccessionLawMapper(reader);

		var ck3Laws = mapper.GetCK3LawsForImperatorLaws(new SortedSet<string> { "madeUpLaw" });
		Assert.Empty(ck3Laws);
	}

	[Fact]
	public void Ck3LawCanBeFound() {
		var reader = new BufferedReader("link = { imp = implaw ck3 = ck3law }");
		var mapper = new SuccessionLawMapper(reader);

		var ck3Laws = mapper.GetCK3LawsForImperatorLaws(new SortedSet<string> { "implaw" });
		Assert.Equal(new SortedSet<string> { "ck3law" }, ck3Laws);
	}

	[Fact]
	public void LinkWithNoCK3LawResultsInWarning() {
		var output = new StringWriter();
		Console.SetOut(output);

		var reader = new BufferedReader("link = { imp = implaw }");
		_ = new SuccessionLawMapper(reader);

		Assert.Contains("SuccessionLawMapper: link with no CK3 successions laws", output.ToString());
	}

	[Fact]
	public void MultipleLawsCanBeReturned() {
		var reader = new BufferedReader(
			"link = { imp = implaw ck3 = ck3law ck3 = ck3law2 }\n" +
			"link = { imp = implaw ck3 = ck3law3 }\n" +
			"link = { imp = implaw2 ck3 = ck3law4 }\n" +
			"link = { imp = implaw3 ck3 = ck3law5 }\n"
		);
		var mapper = new SuccessionLawMapper(reader);

		var ck3Laws = mapper.GetCK3LawsForImperatorLaws(new SortedSet<string> { "implaw", "implaw3" });
		var expectedReturnedLaws = new SortedSet<string> { "ck3law", "ck3law2", "ck3law3", "ck3law5" };
		Assert.Equal(expectedReturnedLaws, ck3Laws);
	}

	[Fact]
	public void MappingsAreReadFromFile() {
		var mapper = new SuccessionLawMapper("TestFiles/configurables/succession_law_map.txt");
		Assert.Equal(
			new SortedSet<string> { "ck3law1", "ck3law2" },
			mapper.GetCK3LawsForImperatorLaws(new() { "implaw1" })
		);
		Assert.Equal(
			new SortedSet<string> { "ck3law3" },
			mapper.GetCK3LawsForImperatorLaws(new() { "implaw2" })
		);
	}
}