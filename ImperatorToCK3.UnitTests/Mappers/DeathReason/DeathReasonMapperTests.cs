using commonItems;
using ImperatorToCK3.Mappers.DeathReason;
using Xunit;

namespace ImperatorToCK3.UnitTests.Mappers.DeathReason {
	public class DeathReasonMapperTests {
		[Fact]
		public void NonMatchGivesEmptyOptional() {
			var reader = new BufferedReader("link = { ck3 = ck3Reason imp = impReason }");
			var mapper = new DeathReasonMapper(reader);
			var ck3Reason = mapper.GetCK3ReasonForImperatorReason("nonMatchingReason");
			Assert.Null(ck3Reason);
		}
		[Fact]
		public void Ck3ReasonCanBeFound() {
			var reader = new BufferedReader("link = { ck3 = ck3Reason imp = impReason }");
			var mapper = new DeathReasonMapper(reader);
			var ck3Reason = mapper.GetCK3ReasonForImperatorReason("impReason");
			Assert.Equal("ck3Reason", ck3Reason);
		}
		[Fact]
		public void MultipleImpReasonsCanBeInARule() {
			var reader = new BufferedReader("link = { ck3 = ck3Reason imp = impReason imp = impReason2 }");
			var mapper = new DeathReasonMapper(reader);
			var ck3Reason1 = mapper.GetCK3ReasonForImperatorReason("impReason");
			var ck3Reason2 = mapper.GetCK3ReasonForImperatorReason("impReason2");
			Assert.Equal("ck3Reason", ck3Reason1);
			Assert.Equal("ck3Reason", ck3Reason2);
		}
		[Fact]
		public void CorrectRuleMatches() {
			var input = "link = { ck3 = ck3Reason imp = impReason }\n" +
				"link = { ck3 = ck3Reason2 imp = impReason2 }";
			var reader = new BufferedReader(input);
			var mapper = new DeathReasonMapper(reader);
			var ck3Reason2 = mapper.GetCK3ReasonForImperatorReason("impReason2");
			Assert.Equal("ck3Reason2", ck3Reason2);
		}

		[Fact]
		public void MappingsWithNoCK3ReasonAreIgnored() {
			var reader = new BufferedReader(
				"link = { imp = impReason }"
			);
			var mapper = new DeathReasonMapper(reader);

			var ck3Reason = mapper.GetCK3ReasonForImperatorReason("impReason");
			Assert.Null(ck3Reason);
		}
	}
}
