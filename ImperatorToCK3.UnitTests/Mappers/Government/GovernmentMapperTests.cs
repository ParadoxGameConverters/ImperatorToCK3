using Xunit;
using commonItems;
using ImperatorToCK3.Mappers.Government;

namespace ImperatorToCK3.UnitTests.Mappers.Government {
    public class GovernmentMapperTests {
        [Fact]
        public void NonMatchGivesNull() {
            var reader = new BufferedReader("link = { ck3 = ck3Government imp = impGovernment }");
            var mapper = new GovernmentMapper(reader);
            var ck3Gov = mapper.GetCK3GovernmentForImperatorGovernment("nonMatchingGovernment");
            Assert.Null(ck3Gov);
        }
        [Fact]
        public void Ck3GovernmentCanBeFound() {
            var reader = new BufferedReader("link = { ck3 = ck3Government imp = impGovernment }");
            var mapper = new GovernmentMapper(reader);
            var ck3Gov = mapper.GetCK3GovernmentForImperatorGovernment("impGovernment");
            Assert.Equal("ck3Government", ck3Gov);
        }
        [Fact]
        public void MultipleImpGovernmentsCanBeInARule() {
            var reader = new BufferedReader("link = { ck3 = ck3Government imp = impGovernment imp = impGovernment2 }");
            var mapper = new GovernmentMapper(reader);
            var ck3Gov1 = mapper.GetCK3GovernmentForImperatorGovernment("impGovernment");
            var ck3Gov2 = mapper.GetCK3GovernmentForImperatorGovernment("impGovernment2");
            Assert.Equal("ck3Government", ck3Gov1);
            Assert.Equal("ck3Government", ck3Gov2);
        }
        [Fact]
        public void CorrectRuleMatches() {
            var reader = new BufferedReader(
                "link = { ck3 = ck3Government imp = impGovernment }\n" +
                "link = { ck3 = ck3Government2 imp = impGovernment2 }"
            );
            var mapper = new GovernmentMapper(reader);
            var ck3Gov = mapper.GetCK3GovernmentForImperatorGovernment("impGovernment2");
            Assert.Equal("ck3Government2", ck3Gov);
        }
    }
}
