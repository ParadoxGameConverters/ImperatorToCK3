using commonItems;
using ImperatorToCK3.CommonUtils.Genes;
using Xunit;

namespace ImperatorToCK3.UnitTests.CommonUtils.Genes {
	[Collection("Sequential")]
	[CollectionDefinition("Sequential", DisableParallelization = true)]
	public class AccessoryGenesTests {
		/*
		[Fact]
		public void IndexCanBeSet() {
			var reader = new BufferedReader(
				"= {\n" +
				"\tindex=69" +
				"}"
			);
			var genes = new AccessoryGenes(reader);

			Assert.Equal((uint)69, genes.Index);
		}*/

		[Fact]
		public void IndexDefaultsToNull() {
			var reader = new BufferedReader("= {}");
			var genes = new AccessoryGenes();
			genes.LoadGenes(reader);

			Assert.Null(genes.Index);
		}

		[Fact]
		public void GenesDefaultToEmpty() {
			var reader = new BufferedReader("= {}");
			var genes = new AccessoryGenes();
			genes.LoadGenes(reader);

			Assert.Empty(genes.Genes);
		}

		[Fact]
		public void AccessoryGenesAreProperlyLoaded() {
			var reader = new BufferedReader(
				"= {\n" +
				"\thairstyles = {\n" +
				"\t\tindex = 1\n" +
				"\t}\n" +
				"\tclothes = {\n" +
				"\t\tindex = 2\n" +
				"\t}\n" +
				"}"
			);
			var genes = new AccessoryGenes();
			genes.LoadGenes(reader);
			Assert.Equal(2, genes.Genes.Count);
			Assert.Equal((uint)1, genes.Genes["hairstyles"].Index);
			Assert.Equal((uint)2, genes.Genes["clothes"].Index);
		}
	}
}
