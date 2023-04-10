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
}