using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using commonItems;
using Xunit;

namespace ImperatorToCK3.UnitTests {
    public class PopTests {
        [Fact] public void IDCanBeSet() {
            var reader = new BufferedReader(" = {}");
            var thePop = new Imperator.Pops.PopFactory().GetPop("42", reader);
            Assert.Equal((ulong)42, thePop.ID);
        }
        [Fact]
        public void CultureCanBeSet() {
            var reader = new BufferedReader(" = { culture=\"paradoxian\" }");
            var thePop = new Imperator.Pops.PopFactory().GetPop("42", reader);
            Assert.Equal("paradoxian", thePop.Culture);
        }
        [Fact]
        public void CultureDefaultsToBlank() {
            var reader = new BufferedReader(" = { }");
            var thePop = new Imperator.Pops.PopFactory().GetPop("42", reader);
            Assert.True(string.IsNullOrEmpty(thePop.Culture));
        }
        [Fact]
        public void ReligionCanBeSet() {
            var reader = new BufferedReader(" = { religion=\"paradoxian\" }");
            var thePop = new Imperator.Pops.PopFactory().GetPop("42", reader);
            Assert.Equal("paradoxian", thePop.Religion);
        }
        [Fact]
        public void ReligionDefaultsToBlank() {
            var reader = new BufferedReader(" = { }");
            var thePop = new Imperator.Pops.PopFactory().GetPop("42", reader);
            Assert.True(string.IsNullOrEmpty(thePop.Religion));
        }
        [Fact]
        public void TypeCanBeSet() {
            var reader = new BufferedReader(" = { type=\"citizen\" }");
            var thePop = new Imperator.Pops.PopFactory().GetPop("42", reader);
            Assert.Equal("citizen", thePop.Type);
        }
        [Fact]
        public void TypeDefaultsToBlank() {
            var reader = new BufferedReader(" = { }");
            var thePop = new Imperator.Pops.PopFactory().GetPop("42", reader);
            Assert.True(string.IsNullOrEmpty(thePop.Type));
        }
    }
}
