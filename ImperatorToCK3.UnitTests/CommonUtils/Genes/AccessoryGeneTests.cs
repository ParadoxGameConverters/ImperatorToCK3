using commonItems;
using ImperatorToCK3.CommonUtils.Genes;
using System.Linq;
using Xunit;

namespace ImperatorToCK3.UnitTests.CommonUtils.Genes; 

public class AccessoryGeneTests {
	[Fact]
	public void IndexCanBeSet() {
		var reader = new BufferedReader(
			"= { index=69 }"
		);
		var gene = new AccessoryGene("test_gene", reader);

		Assert.Equal((uint)69, gene.Index);
	}

	[Fact]
	public void IndexDefaultsToNull() { // special_genes accessory genes don't have an index
		var reader = new BufferedReader("={}");
		var gene = new AccessoryGene("test_gene", reader);

		Assert.Null(gene.Index);
	}

	[Fact]
	public void GeneTemplatesDefaultToEmpty() {
		var reader = new BufferedReader("={}");
		var gene = new AccessoryGene("test_gene", reader);

		Assert.Empty(gene.GeneTemplates);
	}

	[Fact]
	public void AccessoryGeneIsProperlyLoaded() {
		var reader = new BufferedReader(
			" = {\n" +
			"	index = 95\n" +
			"	inheritable = no\n" +
			"	nerdy_hairstyles = {\n" +
			"			index = 0\n" +
			"			male = {\n" +
			"				6 = male_hair_roman_5\n" +
			"				1 = empty\n" +
			"			}\n" +
			"			female = {\n" +
			"				1 = female_hair_roman_1\n" +
			"				1 = female_hair_roman_5\n" +
			"			}\n" +
			"			boy = male\n" +
			"			girl = {\n" +
			"				1 = female_hair_roman_1\n" +
			"				1 = female_hair_roman_5\n" +
			"			}\n" +
			"	}\n" +
			"	punk_hairstyles = {\n" +
			"		index = 1\n" +
			"		male = {\n" +
			"				6 = male_hair_roman_1\n" +
			"				1 = empty\n" +
			"		}\n" +
			"		female = {\n" +
			"				1 = female_hair_roman_1\n" +
			"				1 = female_hair_roman_2\n" +
			"		}\n" +
			"		girl = female\n" +
			"	} \n" +
			"}\n"
		);
		var gene = new AccessoryGene("test_gene", reader);

		Assert.Equal((uint)95, gene.Index);
		Assert.False(gene.Inheritable);
		Assert.Equal(2, gene.GeneTemplates.Count);
		Assert.Equal((uint)1, gene.GeneTemplates["punk_hairstyles"].Index);
		Assert.Equal(4, gene.GeneTemplates["nerdy_hairstyles"].AgeSexWeightBlocks.Count);
		Assert.Equal(3, gene.GeneTemplates["punk_hairstyles"].AgeSexWeightBlocks.Count);
	}

	[Fact]
	public void GetGeneTemplateByIndexReturnsMatch() {
		var reader = new BufferedReader(
			" = {\n" +
			"  hat_template = { index = 0 }\n" +
			"  hood_template = { index = 3 }\n" +
			" }\n"
		);
		var gene = new AccessoryGene("headgear", reader);

		var match = gene.GetGeneTemplateByIndex(3);
		Assert.Equal("hood_template", match.Id);
	}

	[Fact]
	public void GetGeneTemplateByIndexReturnsFirstOnMiss() {
		var reader = new BufferedReader(
			" = {\n" +
			"  a_template = { index = 0 }\n" +
			"  b_template = { index = 1 }\n" +
			" }\n"
		);
		var gene = new AccessoryGene("some_accessory", reader);

		var expectedFallback = gene.GeneTemplates.First();
		var result = gene.GetGeneTemplateByIndex(42);

		Assert.Same(expectedFallback, result);
	}

	[Fact]
	public void GetGeneTemplateByIndexThrowsWhenNoTemplates() {
		var reader = new BufferedReader(" = { index = 5 } ");
		var gene = new AccessoryGene("empty_accessory", reader);

		Assert.Throws<System.InvalidOperationException>(() => gene.GetGeneTemplateByIndex(0));
	}
}