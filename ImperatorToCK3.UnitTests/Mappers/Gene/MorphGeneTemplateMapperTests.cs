using ImperatorToCK3.Mappers.Gene;
using System;
using System.IO;
using Xunit;

namespace ImperatorToCK3.UnitTests.Mappers.Gene;

[Collection("Sequential")]
[CollectionDefinition("Sequential", DisableParallelization = true)]
public class MorphGeneTemplateMapperTests {
	[Fact]
	public void GetCK3TemplateReturnsMappedTemplateOnMatch() {
		var mapper = new MorphGeneTemplateMapper("TestFiles/MapperTests/Gene/morph_gene_templates_map.txt");

		Assert.Equal("ck3_template_1", mapper.GetCK3Template("head_gene", "ir_template_1"));
		Assert.Equal("ck3_template_2", mapper.GetCK3Template("head_gene", "ir_template_2"));
	}

	[Fact]
	public void GetCK3TemplateReturnsNullAndLogsForUnknownGene() {
		var output = new StringWriter();
		Console.SetOut(output);

		var mapper = new MorphGeneTemplateMapper("TestFiles/MapperTests/Gene/morph_gene_templates_map.txt");

		Assert.Null(mapper.GetCK3Template("unknown_gene", "ir_template_1"));
		Assert.Contains("[WARN] I:R gene unknown_gene not found in morph gene template mappings!", output.ToString());
	}

	[Fact]
	public void GetCK3TemplateReturnsNullAndLogsForUnknownTemplate() {
		var output = new StringWriter();
		Console.SetOut(output);

		var mapper = new MorphGeneTemplateMapper("TestFiles/MapperTests/Gene/morph_gene_templates_map.txt");

		Assert.Null(mapper.GetCK3Template("head_gene", "unknown_template"));
		Assert.Contains("[WARN] I:R template unknown_template not found in morph gene template mappings!", output.ToString());
	}
}
