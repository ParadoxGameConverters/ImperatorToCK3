using System.Linq;
using commonItems;
using ImperatorToCK3.CommonUtils.Genes;
using Xunit;

namespace ImperatorToCK3.UnitTests.CommonUtils.Genes;

public class MorphGeneTests {
    [Fact]
    public void IndexCanBeSet() {
        var reader = new BufferedReader("= { index = 12 }");
        var gene = new MorphGene("test_morph_gene", reader);

        Assert.Equal((uint)12, gene.Index);
    }

    [Fact]
    public void IndexDefaultsToNull() {
        var reader = new BufferedReader("= {}");
        var gene = new MorphGene("test_morph_gene", reader);

        Assert.Null(gene.Index);
    }

    [Fact]
    public void GeneTemplatesDefaultToEmpty() {
        var reader = new BufferedReader("= {}");
        var gene = new MorphGene("test_morph_gene", reader);

        Assert.Empty(gene.GeneTemplates);
    }

    [Fact]
    public void MorphGeneIsProperlyLoaded() {
        var reader = new BufferedReader(
            " = {\n" +
            "  index = 7\n" +
            "  nose_morph_01 = { index = 0 visible = yes }\n" +
            "  nose_morph_02 = { index = 3 visible = no }\n" +
            " }\n"
        );
        var gene = new MorphGene("nose_shape", reader);

        Assert.Equal((uint)7, gene.Index);
        Assert.Equal(2, gene.GeneTemplates.Count);
        Assert.Equal((uint)0, gene.GeneTemplates["nose_morph_01"].Index);
        Assert.Equal((uint)3, gene.GeneTemplates["nose_morph_02"].Index);
    }

    [Fact]
    public void GetGeneTemplateByIndexReturnsMatch() {
        var reader = new BufferedReader(
            " = {\n" +
            "  nose_morph_01 = { index = 0 }\n" +
            "  nose_morph_02 = { index = 3 }\n" +
            " }\n"
        );
        var gene = new MorphGene("nose_shape", reader);

        var match = gene.GetGeneTemplateByIndex(3);
        Assert.NotNull(match);
        Assert.Equal("nose_morph_02", match!.Id);
    }

    [Fact]
    public void GetGeneTemplateByIndexReturnsFirstOnMiss() {
        var reader = new BufferedReader(
            " = {\n" +
            "  a_template = { index = 0 }\n" +
            "  b_template = { index = 1 }\n" +
            " }\n"
        );
        var gene = new MorphGene("some_morph", reader);

        var expectedFallback = gene.GeneTemplates.First();
        var result = gene.GetGeneTemplateByIndex(42);

        Assert.Same(expectedFallback, result);
    }

    [Fact]
    public void GetGeneTemplateByIndexReturnsNullWhenNoTemplates() {
        var reader = new BufferedReader(" = { index = 5 } ");
        var gene = new MorphGene("empty_morph", reader);

        var result = gene.GetGeneTemplateByIndex(0);
        Assert.Null(result);
    }
}
