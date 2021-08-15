using System.Collections.Generic;
using Xunit;
using ImperatorToCK3.Mappers.SuccessionLaw;
using commonItems;

namespace ImperatorToCK3.UnitTests.Mappers.SuccessionLaw {
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
	}
}
