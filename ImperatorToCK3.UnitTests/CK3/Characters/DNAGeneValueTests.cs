using ImperatorToCK3.CK3.Characters;
using Xunit;

namespace ImperatorToCK3.UnitTests.CK3.Characters; 

public class DNAGeneValueTests {
	[Fact]
	public void DNAGeneValueIsInitialized() {
		var accessoryGeneValue = new DNAGeneValue {
			TemplateName = "template_name",
			IntSliderValue = 1,
			TemplateRecessiveName = "template_recessive_name",
			IntSliderValueRecessive = 2
		};
		Assert.Equal("template_name", accessoryGeneValue.TemplateName);
		Assert.Equal(1, accessoryGeneValue.IntSliderValue);
		Assert.Equal("template_recessive_name", accessoryGeneValue.TemplateRecessiveName);
		Assert.Equal(2, accessoryGeneValue.IntSliderValueRecessive);
	}
	
	[Fact]
	public void DNAGeneValueIsCorrectlyConvertedToString() {
		var accessoryGeneValue = new DNAGeneValue {
			TemplateName = "template_name",
			IntSliderValue = 1,
			TemplateRecessiveName = "template_recessive_name",
			IntSliderValueRecessive = 2
		};
		Assert.Equal("\"template_name\" 1 \"template_recessive_name\" 2", accessoryGeneValue.ToString());
	}

	[Fact]
	public void DNAGeneValueEqualsReturnsTrueForEqualValues() {
		var a = MakeGeneValue();
		var b = MakeGeneValue();

		Assert.True(a.Equals(b));
		Assert.True(a.Equals((object)b));
	}

	[Fact]
	public void DNAGeneValueEqualsReturnsFalseForDifferentValues() {
		var a = MakeGeneValue();
		var b = new DNAGeneValue {
			TemplateName = "other",
			IntSliderValue = 1,
			TemplateRecessiveName = "template_recessive_name",
			IntSliderValueRecessive = 2
		};

		Assert.False(a.Equals(b));
	}

	[Fact]
	public void DNAGeneValueEqualsReturnsFalseForNonGeneValue() {
		var a = MakeGeneValue();

		Assert.False(a.Equals(null));
		Assert.False(a.Equals("not a gene value"));
		Assert.False(a.Equals(new object()));
	}

	[Fact]
	public void DNAGeneValueGetHashCodeIsEqualForEqualValues() {
		var a = MakeGeneValue();
		var b = MakeGeneValue();

		Assert.Equal(a.GetHashCode(), b.GetHashCode());
	}

	[Fact]
	public void DNAGeneValueEqualityOperatorReturnsTrueForEqualValues() {
		var a = MakeGeneValue();
		var b = MakeGeneValue();

		Assert.True(a == b);
		Assert.False(a != b);
	}

	[Fact]
	public void DNAGeneValueInequalityOperatorReturnsTrueForDifferentValues() {
		var a = MakeGeneValue();
		var b = new DNAGeneValue {
			TemplateName = "other",
			IntSliderValue = 1,
			TemplateRecessiveName = "template_recessive_name",
			IntSliderValueRecessive = 2
		};

		Assert.True(a != b);
		Assert.False(a == b);
	}

	private static DNAGeneValue MakeGeneValue() {
		return new DNAGeneValue {
			TemplateName = "template_name",
			IntSliderValue = 1,
			TemplateRecessiveName = "template_recessive_name",
			IntSliderValueRecessive = 2
		};
	}
}