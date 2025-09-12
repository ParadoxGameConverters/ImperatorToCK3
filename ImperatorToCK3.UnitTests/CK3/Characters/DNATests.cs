using ImperatorToCK3.CK3.Characters;
using ImperatorToCK3.CommonUtils.Genes;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace ImperatorToCK3.UnitTests.CK3.Characters;

public class DNATests {
	private static DNA CreateSampleDNA(
		out Dictionary<string, DNAColorGeneValue> colorDict,
		out Dictionary<string, DNAGeneValue> morphDict,
		out Dictionary<string, DNAAccessoryGeneValue> accessoryDict
	) {
		colorDict = new Dictionary<string, DNAColorGeneValue> {
			{
				"hair_color",
				new DNAColorGeneValue { X = 1, Y = 2, XRecessive = 3, YRecessive = 4 }
			}
		};
		morphDict = new Dictionary<string, DNAGeneValue> {
			{
				"gene_head_height",
				new DNAGeneValue { TemplateName = "tmpl_dom", IntSliderValue = 1, TemplateRecessiveName = "tmpl_rec", IntSliderValueRecessive = 2 }
			}
		};
		var wb = new WeightBlock();
		wb.AddObject("obj_a", 1);
		wb.AddObject("obj_b", 1);
		accessoryDict = new Dictionary<string, DNAAccessoryGeneValue> {
			{ "hairstyles", new DNAAccessoryGeneValue("tmpl_acc", "obj_a", wb) }
		};

		return new DNA("test_dna", colorDict, morphDict, accessoryDict);
	}

	[Fact]
	public void IdPropertyReturnsSuppliedId() {
		var dna = new DNA(
			"my_id",
			new Dictionary<string, DNAColorGeneValue>(),
			new Dictionary<string, DNAGeneValue>(),
			new Dictionary<string, DNAAccessoryGeneValue>()
		);
		Assert.Equal("my_id", dna.Id);
	}

	[Fact]
	public void AccessoryDNAValuesReturnsExpectedEntries() {
		var dna = CreateSampleDNA(out _, out _, out var accessoryInput);

		Assert.Single(dna.AccessoryDNAValues);
		Assert.True(dna.AccessoryDNAValues.ContainsKey("hairstyles"));
		var v = dna.AccessoryDNAValues["hairstyles"];
		Assert.Equal("tmpl_acc", v.TemplateName);
		Assert.Equal("obj_a", v.ObjectName);

		// Mutate original dictionary; dna should not change (constructor copies).
		accessoryInput["beards"] = v;
		Assert.Single(dna.AccessoryDNAValues);
		Assert.DoesNotContain("beards", dna.AccessoryDNAValues.Keys);
	}

	[Fact]
	public void DNALinesCombinesAllGeneTypes() {
		var dna = CreateSampleDNA(out var color, out var morph, out var accessory);

		var lines = dna.DNALines.ToList();
		Assert.Equal(3, lines.Count);

		Assert.Contains("hair_color={ 1 2 3 4 }", lines);
		Assert.Contains("gene_head_height={ \"tmpl_dom\" 1 \"tmpl_rec\" 2 }", lines);
		// For object "obj_a" being the first entry in weight block, slider=0.
		Assert.Contains("hairstyles={ \"tmpl_acc\" 0 \"tmpl_acc\" 0 }", lines);

		// Mutate source dicts; dna should not reflect these changes.
		color["skin_color"] = new DNAColorGeneValue { X = 9, Y = 9, XRecessive = 9, YRecessive = 9 };
		morph["gene_age"] = new DNAGeneValue { TemplateName = "age", IntSliderValue = 5, TemplateRecessiveName = "age", IntSliderValueRecessive = 5 };
		accessory["beards"] = accessory["hairstyles"];

		var linesAfter = dna.DNALines.ToList();
		Assert.Equal(3, linesAfter.Count);
		Assert.DoesNotContain(linesAfter, l => l.StartsWith("skin_color="));
		Assert.DoesNotContain(linesAfter, l => l.StartsWith("gene_age="));
		Assert.DoesNotContain(linesAfter, l => l.StartsWith("beards="));
	}

	[Fact]
	public void ConstructorCopiesInputDictionaries() {
		var dna = CreateSampleDNA(out var color, out var morph, out var accessory);

		// Replace entire dictionaries' contents after construction.
		color.Clear();
		morph.Clear();
		accessory.Clear();

		// DNA should still expose original values.
		Assert.Single(dna.AccessoryDNAValues);
		Assert.Equal(3, dna.DNALines.Count());
	}

	[Fact]
	public void WriteGenesProducesExpectedBlockWithEntries() {
		var dna = CreateSampleDNA(out _, out _, out _);
		var sb = new StringBuilder();
		dna.WriteGenes(sb);

		var output = sb.ToString();
		// Normalize newlines for assertions.
		var lines = output.Replace("\r\n", "\n").Split('\n');
		// Opening and closing braces present
		Assert.Contains("\t\tgenes={", lines);
		Assert.Contains("\t\t}", lines);

		// Three inner lines written, each indented once more
		var inner = lines.Where(l => l.StartsWith("\t\t\t")).ToList();
		Assert.Equal(3, inner.Count);
		Assert.Contains("\t\t\thair_color={ 1 2 3 4 }", inner);
		Assert.Contains("\t\t\tgene_head_height={ \"tmpl_dom\" 1 \"tmpl_rec\" 2 }", inner);
		Assert.Contains("\t\t\thairstyles={ \"tmpl_acc\" 0 \"tmpl_acc\" 0 }", inner);
	}

	[Fact]
	public void WriteGenesWritesEmptyBlockWhenNoGenes() {
		var dna = new DNA(
			"empty",
			new Dictionary<string, DNAColorGeneValue>(),
			new Dictionary<string, DNAGeneValue>(),
			new Dictionary<string, DNAAccessoryGeneValue>()
		);
		var sb = new StringBuilder();
		dna.WriteGenes(sb);

		var output = sb.ToString();
		var lines = output.Replace("\r\n", "\n").Split('\n');
		Assert.Contains("\t\tgenes={", lines);
		Assert.Contains("\t\t}", lines);
		Assert.DoesNotContain(lines, l => l.StartsWith("\t\t\t"));
	}
}
