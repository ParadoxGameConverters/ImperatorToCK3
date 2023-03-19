using commonItems;
using ImperatorToCK3.CommonUtils.Genes;
using Xunit;

namespace ImperatorToCK3.UnitTests.CommonUtils.Genes; 

public class AccessoryGeneTests {
	[Fact]
	public void IndexCanBeSet() {
		var reader = new BufferedReader(
			"=\n" +
			"{\n" +
			"\tindex=69" +
			"}"
		);
		var gene = new AccessoryGene(reader);

		Assert.Equal((uint)69, gene.Index);
	}

	[Fact]
	public void IndexDefaultsToNull() { // special_genes accessory genes don't have an index
		var reader = new BufferedReader("={}");
		var gene = new AccessoryGene(reader);

		Assert.Null(gene.Index);
	}

	[Fact]
	public void GeneTemplatesDefaultToEmpty() {
		var reader = new BufferedReader("={}");
		var gene = new AccessoryGene(reader);

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
		var gene = new AccessoryGene(reader);

		Assert.Equal((uint)95, gene.Index);
		Assert.False(gene.Inheritable);
		Assert.Equal(2, gene.GeneTemplates.Count);
		Assert.Equal((uint)1, gene.GeneTemplates["punk_hairstyles"].Index);
		Assert.Equal(4, gene.GeneTemplates["nerdy_hairstyles"].AgeSexWeightBlocks.Count);
		Assert.Equal(3, gene.GeneTemplates["punk_hairstyles"].AgeSexWeightBlocks.Count);
	}
}