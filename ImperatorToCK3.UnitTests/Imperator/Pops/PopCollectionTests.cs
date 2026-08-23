using commonItems;
using ImperatorToCK3.Imperator.Pops;
using Xunit;

namespace ImperatorToCK3.UnitTests.Imperator.Pops;

public class PopCollectionTests {
	[Fact]
	public void PopsDefaultToEmptyWhenLoadedFromBloc() {
		var reader = new BufferedReader("= { }");
		var pops = new PopCollection();
		pops.LoadPopsFromBloc(reader);

		Assert.Empty(pops);
	}

	[Fact]
	public void PopsCanBeLoadedFromBloc() {
		var reader = new BufferedReader(
			"""
			= {
				population = {
					42 = { type="citizen" culture="roman" religion="paradoxian" }
					43 = { type="tribal" culture="persian" religion="gsg" }
				}
			}
			"""
		);
		var pops = new PopCollection();
		pops.LoadPopsFromBloc(reader);

		Assert.Equal(2, pops.Count);
		var pop1 = pops[42];
		Assert.Equal((ulong)42, pop1.Id);
		Assert.Equal("citizen", pop1.Type);
		Assert.Equal("roman", pop1.Culture);
		Assert.Equal("paradoxian", pop1.Religion);
		var pop2 = pops[43];
		Assert.Equal((ulong)43, pop2.Id);
		Assert.Equal("tribal", pop2.Type);
		Assert.Equal("persian", pop2.Culture);
		Assert.Equal("gsg", pop2.Religion);
	}

	[Fact]
	public void PopsFromMultiplePopulationBlocsCanBeLoaded() {
		var reader = new BufferedReader(
			"""
			= {
				population = {
					42 = { type="citizen" culture="roman" religion="paradoxian" }
				}
				population = {
					43 = { type="tribal" culture="persian" religion="gsg" }
				}
			}
			"""
		);
		var pops = new PopCollection();
		pops.LoadPopsFromBloc(reader);

		Assert.Equal(2, pops.Count);
		Assert.Equal("citizen", pops[42].Type);
		Assert.Equal("tribal", pops[43].Type);
	}

	[Fact]
	public void UnregisteredKeywordsInBlocAreIgnored() {
		var reader = new BufferedReader(
			"""
			= {
				something_else = {
					1 = some_value
				}
				population = {
					42 = { type="citizen" }
				}
			}
			"""
		);
		var pops = new PopCollection();
		pops.LoadPopsFromBloc(reader);

		var pop = Assert.Single(pops);
		Assert.Equal((ulong)42, pop.Id);
		Assert.Equal("citizen", pop.Type);
	}

	[Fact]
	public void LiteralNonePopsAreNotLoadedFromBloc() {
		var reader = new BufferedReader(
			"""
			= {
				population = {
					42 = none
					43 = { type="citizen" }
					44 = none
				}
			}
			"""
		);
		var pops = new PopCollection();
		pops.LoadPopsFromBloc(reader);

		Assert.Single(pops);
		Assert.False(pops.ContainsKey(42));
		Assert.True(pops.ContainsKey(43));
		Assert.False(pops.ContainsKey(44));
		Assert.Equal((ulong)43, pops[43].Id);
	}

	[Fact]
	public void PopsWithoutBracedDataAreNotLoadedFromBloc() {
		var reader = new BufferedReader(
			"""
			= {
				population = {
					42 = 0
					43 = { type="citizen" }
				}
			}
			"""
		);
		var pops = new PopCollection();
		pops.LoadPopsFromBloc(reader);

		var pop = Assert.Single(pops);
		Assert.Equal((ulong)43, pop.Id);
	}
}