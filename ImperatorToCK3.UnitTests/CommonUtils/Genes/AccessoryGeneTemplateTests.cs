using commonItems;
using ImperatorToCK3.CommonUtils.Genes;
using Xunit;

namespace ImperatorToCK3.UnitTests.CommonUtils.Genes {
	public class AccessoryGeneTemplateTests {
		[Fact]
		public void AgeSexWeightBlocksDefaultsToEmpty() {
			var reader = new BufferedReader(
				"= {}"
			);
			var geneTemplate = new AccessoryGeneTemplate(reader);

			Assert.Empty(geneTemplate.AgeSexWeightBlocks);
		}

		[Fact]
		public void AgeSexWeightBlocksCanBeLoaded() {
			var reader = new BufferedReader(
				"={\n" +
				"male={}\n" +
				"female={}\n" +
				"boy=male\n" +
				"girl=female\n" +
				"}"
			);
			var geneTemplate = new AccessoryGeneTemplate(reader);

			Assert.Equal(4, geneTemplate.AgeSexWeightBlocks.Count);
		}

		[Fact]
		public void AgeSexWithBlocksAreProperlyCopied() {
			var reader = new BufferedReader(
				"=\n" +
				"{\n" +
				"male={ 6 = hoodie 8 = trousers }\n" +
				"female={ 4 = skirt 6 = top }\n" +
				"boy=male\n" +
				"girl=female\n" +
				"}"
			);
			var geneTemplate = new AccessoryGeneTemplate(reader);

			Assert.Equal((uint)6, geneTemplate.AgeSexWeightBlocks["male"].GetAbsoluteWeight("hoodie"));
			Assert.Equal((uint)8, geneTemplate.AgeSexWeightBlocks["male"].GetAbsoluteWeight("trousers"));
			Assert.Equal((uint)4, geneTemplate.AgeSexWeightBlocks["female"].GetAbsoluteWeight("skirt"));
			Assert.Equal((uint)6, geneTemplate.AgeSexWeightBlocks["female"].GetAbsoluteWeight("top"));
			Assert.Equal((uint)6, geneTemplate.AgeSexWeightBlocks["boy"].GetAbsoluteWeight("hoodie"));
			Assert.Equal((uint)8, geneTemplate.AgeSexWeightBlocks["boy"].GetAbsoluteWeight("trousers"));
			Assert.Equal((uint)4, geneTemplate.AgeSexWeightBlocks["girl"].GetAbsoluteWeight("skirt"));
			Assert.Equal((uint)6, geneTemplate.AgeSexWeightBlocks["girl"].GetAbsoluteWeight("top"));
		}
	}
}
