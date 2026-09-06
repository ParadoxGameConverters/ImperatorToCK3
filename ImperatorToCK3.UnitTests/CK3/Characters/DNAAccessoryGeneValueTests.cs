using ImperatorToCK3.CK3.Characters;
using ImperatorToCK3.CommonUtils.Genes;
using Xunit;

namespace ImperatorToCK3.UnitTests.CK3.Characters;

public class DNAAccessoryGeneValueTests {
	private static WeightBlock MakeWeightBlock() {
		var weightBlock = new WeightBlock();
		weightBlock.AddObject("obj_a", 3);
		weightBlock.AddObject("obj_b", 1);
		return weightBlock;
	}

	private static DNAAccessoryGeneValue MakeGeneValue() {
		return new DNAAccessoryGeneValue(
			"template_name",
			"obj_b",
			MakeWeightBlock(),
			"template_recessive_name",
			"obj_a",
			MakeWeightBlock()
		);
	}

	[Fact]
	public void ThreeArgCtor_UsesSameTemplateAndObjectForRecessive() {
		var weightBlock = MakeWeightBlock();

		var geneValue = new DNAAccessoryGeneValue("template_name", "obj_b", weightBlock);

		Assert.Equal("template_name", geneValue.TemplateName);
		Assert.Equal("obj_b", geneValue.ObjectName);
		Assert.Equal("template_name", geneValue.TemplateRecessiveName);
		Assert.Equal("obj_b", geneValue.ObjectRecessiveName);
		Assert.Equal(geneValue.IntSliderValue, geneValue.IntSliderValueRecessive);
	}

	[Fact]
	public void IntSliderValue_ReturnsValueForKnownObject() {
		var geneValue = MakeGeneValue();

		// obj_b has 1/4 of total weight, preceding weight is 3: 3/4*255 = 191.25, ceiling = 192.
		Assert.Equal(192, geneValue.IntSliderValue);
	}

	[Fact]
	public void IntSliderValue_ReturnsZeroForUnknownObject() {
		var geneValue = new DNAAccessoryGeneValue(
			"template_name",
			"unknown_obj",
			MakeWeightBlock()
		);

		Assert.Equal(0, geneValue.IntSliderValue);
	}

	[Fact]
	public void IntSliderValueRecessive_ReturnsValueForKnownObject() {
		var geneValue = MakeGeneValue();

		// obj_a is the first object: 0/4*255 = 0.
		Assert.Equal(0, geneValue.IntSliderValueRecessive);
	}

	[Fact]
	public void IntSliderValueRecessive_ReturnsZeroForUnknownObject() {
		var geneValue = new DNAAccessoryGeneValue(
			"template_name",
			"obj_b",
			MakeWeightBlock(),
			"template_recessive_name",
			"unknown_obj",
			MakeWeightBlock()
		);

		Assert.Equal(0, geneValue.IntSliderValueRecessive);
	}

	[Fact]
	public void SliderValueBetween0And1_ScalesIntSliderValueBy255() {
		var geneValue = MakeGeneValue();

		Assert.Equal(192 / 255.0, geneValue.SliderValueBetween0And1);
	}

	[Fact]
	public void ToString_ReturnsExpectedFormat() {
		var geneValue = MakeGeneValue();

		Assert.Equal("\"template_name\" 192 \"template_recessive_name\" 0", geneValue.ToString());
	}

	[Fact]
	public void Equals_ReturnsTrueForEqualValues() {
		var a = MakeGeneValue();
		var b = MakeGeneValue();

		Assert.True(a.Equals(b));
		Assert.True(a.Equals((object)b));
	}

	[Fact]
	public void Equals_ReturnsFalseWhenTemplateNameDiffers() {
		var a = MakeGeneValue();
		var b = new DNAAccessoryGeneValue(
			"other_template",
			"obj_b",
			MakeWeightBlock(),
			"template_recessive_name",
			"obj_a",
			MakeWeightBlock()
		);

		Assert.False(a.Equals(b));
	}

	[Fact]
	public void Equals_ReturnsFalseWhenSliderValueDiffers() {
		var a = MakeGeneValue();
		var b = new DNAAccessoryGeneValue(
			"template_name",
			"obj_a",
			MakeWeightBlock(),
			"template_recessive_name",
			"obj_a",
			MakeWeightBlock()
		);

		Assert.False(a.Equals(b));
	}

	[Fact]
	public void Equals_ReturnsFalseWhenRecessiveTemplateNameDiffers() {
		var a = MakeGeneValue();
		var b = new DNAAccessoryGeneValue(
			"template_name",
			"obj_b",
			MakeWeightBlock(),
			"other_recessive_template",
			"obj_a",
			MakeWeightBlock()
		);

		Assert.False(a.Equals(b));
	}

	[Fact]
	public void Equals_ReturnsFalseWhenRecessiveSliderValueDiffers() {
		var a = MakeGeneValue();
		var b = new DNAAccessoryGeneValue(
			"template_name",
			"obj_b",
			MakeWeightBlock(),
			"template_recessive_name",
			"obj_b",
			MakeWeightBlock()
		);

		Assert.False(a.Equals(b));
	}

	[Fact]
	public void Equals_ReturnsFalseForOtherTypes() {
		var a = MakeGeneValue();

		Assert.False(a.Equals(null));
		Assert.False(a.Equals("not a gene value"));
		Assert.False(a.Equals(new object()));
	}

	[Fact]
	public void GetHashCode_IsEqualForEqualValues() {
		var a = MakeGeneValue();
		var b = MakeGeneValue();

		Assert.Equal(a.GetHashCode(), b.GetHashCode());
	}

	[Fact]
	public void EqualityOperator_ReturnsTrueForEqualValues() {
		var a = MakeGeneValue();
		var b = MakeGeneValue();

		Assert.True(a == b);
		Assert.False(a != b);
	}

	[Fact]
	public void InequalityOperator_ReturnsTrueForDifferentValues() {
		var a = MakeGeneValue();
		var b = new DNAAccessoryGeneValue(
			"other_template",
			"obj_b",
			MakeWeightBlock(),
			"template_recessive_name",
			"obj_a",
			MakeWeightBlock()
		);

		Assert.True(a != b);
		Assert.False(a == b);
	}
}
