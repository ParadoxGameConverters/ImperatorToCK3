using commonItems;
using Xunit;
using ImperatorToCK3.Imperator.Pops;

namespace ImperatorToCK3.UnitTests {
    public class PopTests {
        [Fact] public void EverythingCanBeSet() {
            var reader = new BufferedReader(" = {" +
                "culture=\"paradoxian\"\n" +
                "religion=\"nicene\"\n" +
                "type=\"citizen\"\n" +
                "}");
            var thePop = new PopFactory().GetPop("42", reader);
            Assert.Equal((ulong)42, thePop.ID);
            Assert.Equal("paradoxian", thePop.Culture);
            Assert.Equal("nicene", thePop.Religion);
            Assert.Equal("citizen", thePop.Type);
        }
        [Fact]
        public void EverythingDefaultsToBlank() {
            var reader = new BufferedReader(" = { }");
            var thePop = new PopFactory().GetPop("42", reader);
            Assert.True(string.IsNullOrEmpty(thePop.Culture));
            Assert.True(string.IsNullOrEmpty(thePop.Religion));
            Assert.True(string.IsNullOrEmpty(thePop.Type));
        }
    }
}
