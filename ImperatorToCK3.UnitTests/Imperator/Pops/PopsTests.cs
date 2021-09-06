using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using commonItems;
using ImperatorToCK3.Imperator.Pops;
using Xunit;

namespace ImperatorToCK3.UnitTests {
    public class PopsTests {
        [Fact] public void PopsDefaultToEmpty() {
            var reader = new BufferedReader("= { }");
            var pops = new Pops();
            pops.LoadPops(reader);
            Assert.Empty(pops.StoredPops);
        }
        [Fact]
        public void PopsCanBeLoaded() {
            var reader = new BufferedReader("= {\n 42={}\n 43 = {}\n }");
            var pops = new Pops();
            pops.LoadPops(reader);
            var pop1= pops.StoredPops[42];
            var pop2 = pops.StoredPops[43];
            Assert.Equal(2, pops.StoredPops.Count);
            Assert.Equal((ulong)42, pop1.ID);
            Assert.Equal((ulong)43, pop2.ID);
        }
        [Fact]
        public void LiteralNonePopsAreNotLoaded() {
            var reader = new BufferedReader("= {\n 42=none\n 43={}\n 44=none\n }");
            var pops = new Pops();
            pops.LoadPops(reader);
            Assert.Equal(1, pops.StoredPops.Count);
            Assert.False(pops.StoredPops.ContainsKey(42));
            Assert.True(pops.StoredPops.ContainsKey(43));
            Assert.False(pops.StoredPops.ContainsKey(44));
            var pop2 = pops.StoredPops[43];
            Assert.Equal((ulong)43, pop2.ID);
        }
    }
}
