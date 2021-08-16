using commonItems;
using ImperatorToCK3.Mappers.DeathReason;
using Xunit;

namespace ImperatorToCK3.UnitTests.Mappers.DeathReason {
    public class DeathReasonMappingTests {
        [Fact]
        public void ck3ReasonDefaultsToNullopt() {
            var reader = new BufferedReader("");
            var mapping = new DeathReasonMapping(reader);
            Assert.Null(mapping.Ck3Reason);
        }
        [Fact]
        public void ck3ReasonCanBeSet() {
            var reader = new BufferedReader("= { ck3 = ck3Trait }");
            var mapping = new DeathReasonMapping(reader);
            Assert.Equal("ck3Trait", mapping.Ck3Reason);
        }
        [Fact]
        public void imperatorReasonsDefaultToEmpty() {
            var reader = new BufferedReader("");
            var mapping = new DeathReasonMapping(reader);
            Assert.Empty(mapping.ImpReasons);
        }
        [Fact]
        public void imperatorReasonsCanBeSet() {
            var reader = new BufferedReader("= { imp = reason_dumb imp = reason_bear }");
            var mapping = new DeathReasonMapping(reader);
            Assert.Collection(mapping.ImpReasons,
                item => Assert.Equal("reason_bear", item),
                item => Assert.Equal("reason_dumb", item)
            );
        }
    }
}
