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
            var thePop = Pop.Parse("42", reader);
            Assert.Equal((ulong)42, thePop.ID);
            Assert.Equal("paradoxian", thePop.Culture);
            Assert.Equal("nicene", thePop.Religion);
            Assert.Equal("citizen", thePop.Type);

			var reader2 = new BufferedReader(" = {" +
				"culture=\"paradoxus\"\n" +
				"religion=\"nicenus\"\n" +
				"type=\"citizenus\"\n" +
				"}");
			var thePop2 = Pop.Parse("43", reader2);
			Assert.Equal((ulong)43, thePop2.ID);
			Assert.Equal("paradoxus", thePop2.Culture);
			Assert.Equal("nicenus", thePop2.Religion);
			Assert.Equal("citizenus", thePop2.Type);
		}
        [Fact]
        public void EverythingDefaultsToBlank() {
            var reader = new BufferedReader(" = { }");
            var thePop = Pop.Parse("42", reader);
            Assert.True(string.IsNullOrEmpty(thePop.Culture));
            Assert.True(string.IsNullOrEmpty(thePop.Religion));
            Assert.True(string.IsNullOrEmpty(thePop.Type));
        }
    }
}
