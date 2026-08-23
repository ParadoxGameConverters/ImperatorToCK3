using ImperatorToCK3.Mappers.Gene;
using Xunit;

namespace ImperatorToCK3.UnitTests.Mappers.Gene;

public class AccessoryGeneMapperTests {
	private const string MappingsFilePath = "TestFiles/MapperTests/Gene/accessory_genes_map.txt";

	[Fact]
	public void GetObjectFromObjectReturnsMappedObject() {
		var mapper = new AccessoryGeneMapper(MappingsFilePath);

		var mappedObject = mapper.GetObjectFromObject("hairstyles", "male_hair_roman_1");

		Assert.Equal("male_hair_western_01", mappedObject);
	}

	[Fact]
	public void GetObjectFromObjectReturnsNullForMissingObject() {
		var mapper = new AccessoryGeneMapper(MappingsFilePath);

		var mappedObject = mapper.GetObjectFromObject("hairstyles", "missing_hair");

		Assert.Null(mappedObject);
	}

	[Fact]
	public void GetObjectFromObjectReturnsNullForMissingGene() {
		var mapper = new AccessoryGeneMapper(MappingsFilePath);

		var mappedObject = mapper.GetObjectFromObject("clothes", "roman_clothes");

		Assert.Null(mappedObject);
	}

	[Fact]
	public void GetTemplateFromTemplateReturnsMappedTemplateWhenValidForCK3() {
		var mapper = new AccessoryGeneMapper(MappingsFilePath);

		var mappedTemplate = mapper.GetTemplateFromTemplate(
			"clothes",
			"roman_clothes",
			["western_royalty_clothes", "byzantine_high_nobility_clothes"]
		);

		Assert.Equal("byzantine_high_nobility_clothes", mappedTemplate);
	}

	[Fact]
	public void GetTemplateFromTemplateReturnsFirstValidMappedCk3Template() {
		var mapper = new AccessoryGeneMapper(MappingsFilePath);

		var mappedTemplate = mapper.GetTemplateFromTemplate(
			"clothes",
			"greek_clothes",
			["greek_clothes"] // non_existent_greek_clothes not valid
		);
		Assert.Equal("greek_clothes", mappedTemplate);
	}

	[Fact]
	public void GetTemplateFromTemplateReturnsNullWhenMappedTemplateIsNotValidForCK3() {
		var mapper = new AccessoryGeneMapper(MappingsFilePath);

		var mappedTemplate = mapper.GetTemplateFromTemplate(
			"clothes",
			"roman_clothes",
			["western_royalty_clothes"]
		);

		Assert.Null(mappedTemplate);
	}

	[Fact]
	public void GetTemplateFromTemplateReturnsNullForMissingGene() {
		var mapper = new AccessoryGeneMapper(MappingsFilePath);

		var mappedTemplate = mapper.GetTemplateFromTemplate(
			"beards",
			"male_beard_1",
			["some_beard_template"]
		);

		Assert.Null(mappedTemplate);
	}

	[Fact]
	public void GetFallbackTemplateForGeneReturnsFirstValidTemplate() {
		var mapper = new AccessoryGeneMapper(MappingsFilePath);

		var fallbackTemplate = mapper.GetFallbackTemplateForGene(
			"clothes",
			["no_clothes", "western_royalty_clothes"]
		);

		Assert.Equal("western_royalty_clothes", fallbackTemplate);
	}

	[Fact]
	public void GetFallbackTemplateForGeneReturnsNullForMissingGene() {
		var mapper = new AccessoryGeneMapper(MappingsFilePath);

		var fallbackTemplate = mapper.GetFallbackTemplateForGene(
			"eye_accessory",
			["blind_eyes"]
		);

		Assert.Null(fallbackTemplate);
	}

	[Fact]
	public void GetFallbackTemplateForGeneReturnsNullWhenNoTemplateIsValidForCK3() {
		var mapper = new AccessoryGeneMapper(MappingsFilePath);

		var fallbackTemplate = mapper.GetFallbackTemplateForGene(
			"clothes",
			["template_not_in_mapping"]
		);

		Assert.Null(fallbackTemplate);
	}
}
