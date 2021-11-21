using commonItems;
using ImperatorToCK3.Imperator.Pops;
using Xunit;

namespace ImperatorToCK3.UnitTests {
	public class PopsTests {
		[Fact]
		public void PopsDefaultToEmpty() {
			var reader = new BufferedReader("= { }");
			var pops = new Pops();
			pops.LoadPops(reader);
			Assert.Empty(pops);
		}
		[Fact]
		public void PopsCanBeLoaded() {
			var reader = new BufferedReader("= {\n 42={}\n 43 = {}\n }");
			var pops = new Pops();
			pops.LoadPops(reader);
			var pop1 = pops[42];
			var pop2 = pops[43];
			Assert.Equal(2, pops.Count);
			Assert.Equal((ulong)42, pop1.Id);
			Assert.Equal((ulong)43, pop2.Id);
		}
		[Fact]
		public void LiteralNonePopsAreNotLoaded() {
			var reader = new BufferedReader("= {\n 42=none\n 43={}\n 44=none\n }");
			var pops = new Pops();
			pops.LoadPops(reader);
			Assert.Single(pops);
			Assert.False(pops.ContainsKey(42));
			Assert.True(pops.ContainsKey(43));
			Assert.False(pops.ContainsKey(44));
			var pop2 = pops[43];
			Assert.Equal((ulong)43, pop2.Id);
		}
	}
}
