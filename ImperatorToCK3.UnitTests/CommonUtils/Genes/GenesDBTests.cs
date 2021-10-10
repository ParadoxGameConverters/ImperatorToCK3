using commonItems;
using ImperatorToCK3.CommonUtils.Genes;
using Xunit;

namespace ImperatorToCK3.UnitTests.CommonUtils.Genes {
	public class GenesDBTests {
		[Fact]
		public void GenesDefaultToEmpty() {
			var reader = new BufferedReader("={}");
			var genesDB = new GenesDB(reader);

			Assert.Empty(genesDB.Genes.Genes);
		}

		[Fact]
		public void AccessoryGenesCanBeLoadedInsideGeneGroup() {
			var reader = new BufferedReader(
				"accessory_genes = {\n" +
				"\thairstyles={ index = 1}\n" +
				"\tclothes={ index =2}\n" +
				"}"
			);
			var genesDB = new GenesDB(reader);

			Assert.Equal(2, genesDB.Genes.Genes.Count);
			Assert.Equal((uint)1, genesDB.Genes.Genes["hairstyles"].Index);
			Assert.Equal((uint)2, genesDB.Genes.Genes["clothes"].Index);
		}
	}
}
