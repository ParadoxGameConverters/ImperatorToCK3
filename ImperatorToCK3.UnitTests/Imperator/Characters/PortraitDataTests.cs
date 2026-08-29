using commonItems;
using ImperatorToCK3.CommonUtils.Genes;
using ImperatorToCK3.Imperator.Characters;
using System;
using System.IO;
using Xunit;

namespace ImperatorToCK3.UnitTests.Imperator.Characters;

[Collection("Sequential")]
public class PortraitDataTests {
	private static GenesDB CreateGenesDB() {
		return new GenesDB(new BufferedReader("""
			accessory_genes = {
				acc_no_index = {
					hood_t = { index = 0 male = { 1 = hood_obj } }
				}
				acc_short_dna = {
					index = 45
					hood_t2 = { index = 0 male = { 1 = hood2_obj } }
				}
				test_hats = {
					index = 3
					greek_hood = { index = 0 male = { 1 = male_hood_object 1 = alt_hood_object } }
				}
				female_only_gene = {
					index = 4
					ladies_hat = { index = 0 female = { 1 = ladies_object } }
				}
				recessive_gap_gene = {
					index = 5
					ok_template = { index = 0 male = { 1 = ok_object } }
					no_male_template = { index = 1 female = { 1 = female_object } }
				}
				empty_block_gene = {
					index = 6
					empty_male_template = { index = 0 male = { } }
				}
			}
			morph_genes = {
				expression = {
					index = 0
					ignored_template = { index = 0 }
				}
				morph_no_index = {
					t = { index = 0 }
				}
				morph_short_dna = {
					index = 50
					t2 = { index = 0 }
				}
				morph_empty_templates = {
					index = 7
				}
				test_face = {
					index = 3
					face_template_a = { index = 0 }
					face_template_b = { index = 1 }
				}
			}
			"""));
	}

	private static byte[] CreateTestDna() {
		var dna = new byte[40];
		dna[0] = 100;
		dna[1] = 101;
		dna[2] = 102;
		dna[3] = 103; // hair colors
		dna[4] = 110;
		dna[5] = 111;
		dna[6] = 112;
		dna[7] = 113; // skin colors
		dna[8] = 120;
		dna[9] = 121;
		dna[10] = 122;
		dna[11] = 123; // eye colors
		// Genes with index 3 read bytes 12-15:
		dna[12] = 0; // dominant template index 0
		dna[13] = 77; // dominant value
		dna[14] = 1; // recessive template index 1
		dna[15] = 88; // recessive value
		// recessive_gap_gene has index 5 and reads bytes 20-23:
		dna[20] = 0; // dominant template index 0 (has a male weight block)
		dna[21] = 50;
		dna[22] = 1; // recessive template index 1 (lacks a male weight block)
		dna[23] = 60;
		return dna;
	}

	[Fact]
	public void PortraitDataIsCorrectlyParsed() {
		var genesDB = CreateGenesDB();
		var portraitData = new PortraitData(Convert.ToBase64String(CreateTestDna()), genesDB);

		Assert.Equal(200, portraitData.HairColorPaletteCoordinates.X);
		Assert.Equal(202, portraitData.HairColorPaletteCoordinates.Y);
		Assert.Equal(204, portraitData.HairColor2PaletteCoordinates.X);
		Assert.Equal(206, portraitData.HairColor2PaletteCoordinates.Y);
		Assert.Equal(220, portraitData.SkinColorPaletteCoordinates.X);
		Assert.Equal(222, portraitData.SkinColorPaletteCoordinates.Y);
		Assert.Equal(224, portraitData.SkinColor2PaletteCoordinates.X);
		Assert.Equal(226, portraitData.SkinColor2PaletteCoordinates.Y);
		Assert.Equal(240, portraitData.EyeColorPaletteCoordinates.X);
		Assert.Equal(242, portraitData.EyeColorPaletteCoordinates.Y);
		Assert.Equal(244, portraitData.EyeColor2PaletteCoordinates.X);
		Assert.Equal(246, portraitData.EyeColor2PaletteCoordinates.Y);

		// The ignored "expression" morph gene, gene without index, gene with empty templates
		// and gene whose index points outside the DNA should all be skipped.
		Assert.Single(portraitData.MorphGenesDict);
		var morphGeneData = portraitData.MorphGenesDict["test_face"];
		Assert.Equal("face_template_a", morphGeneData.TemplateName);
		Assert.Equal((byte)77, morphGeneData.Value);
		Assert.Equal("face_template_b", morphGeneData.TemplateRecessiveName);
		Assert.Equal((byte)88, morphGeneData.ValueRecessive);

		// Only test_hats should end up with extracted accessory gene data:
		// female_only_gene has no male weight block, recessive_gap_gene's recessive template
		// has no male weight block and empty_block_gene's male block has no objects.
		Assert.Single(portraitData.AccessoryGenesDict);
		var accessoryGeneData = portraitData.AccessoryGenesDict["test_hats"];
		Assert.Equal("greek_hood", accessoryGeneData.GeneTemplate);
		Assert.Equal("male_hood_object", accessoryGeneData.ObjectName);
		// The recessive template index 1 does not exist in test_hats, so the first template is used as fallback.
		Assert.Equal("greek_hood", accessoryGeneData.GeneTemplateRecessive);
		Assert.Equal("male_hood_object", accessoryGeneData.ObjectNameRecessive);
	}

	[Fact]
	public void EmptyDnaStringIsSkipped() {
		var output = new StringWriter();
		Console.SetOut(output);

		var portraitData = new PortraitData(string.Empty, CreateGenesDB());

		// Palette coordinates keep their default values.
		Assert.Equal(256, portraitData.HairColorPaletteCoordinates.X);
		Assert.Equal(256, portraitData.HairColorPaletteCoordinates.Y);
		Assert.Empty(portraitData.AccessoryGenesDict);
		Assert.Empty(portraitData.MorphGenesDict);
		Assert.Contains("[WARN] DNA string is empty; skipping portrait parsing.", output.ToString());
	}

	[Fact]
	public void WhitespaceDnaStringIsSkipped() {
		var output = new StringWriter();
		Console.SetOut(output);

		var portraitData = new PortraitData(" \t\n", CreateGenesDB());

		Assert.Empty(portraitData.AccessoryGenesDict);
		Assert.Contains("[WARN] DNA string is empty; skipping portrait parsing.", output.ToString());
	}

	[Fact]
	public void UnrecoverableDnaStringIsSkipped() {
		var output = new StringWriter();
		Console.SetOut(output);

		var portraitData = new PortraitData("!!!!", CreateGenesDB());

		Assert.Equal(256, portraitData.EyeColorPaletteCoordinates.X);
		Assert.Empty(portraitData.MorphGenesDict);
		Assert.Contains("Attempting to sanitize and recover", output.ToString());
		Assert.Contains("[WARN] Invalid DNA base64 string for portrait; skipping decoding.", output.ToString());
	}

	[Fact]
	public void DnaStringWrappedInWhitespaceAndQuotesIsSanitized() {
		var output = new StringWriter();
		Console.SetOut(output);

		var goodBase64 = Convert.ToBase64String(CreateTestDna());
		var corruptedDna = $" \"{goodBase64}\" ";

		var portraitData = new PortraitData(corruptedDna, CreateGenesDB());

		Assert.Equal(200, portraitData.HairColorPaletteCoordinates.X);
		Assert.Single(portraitData.MorphGenesDict);
		Assert.Contains("Attempting to sanitize and recover", output.ToString());
	}

	[Fact]
	public void UrlSafeDnaStringIsSanitized() {
		var goodBase64 = Convert.ToBase64String(CreateTestDna());
		var corruptedDna = $"\"{goodBase64.Replace('/', '_').Replace('+', '-')}\"";

		var portraitData = new PortraitData(corruptedDna, CreateGenesDB());

		Assert.Equal(200, portraitData.HairColorPaletteCoordinates.X);
		Assert.Single(portraitData.AccessoryGenesDict);
	}

	[Fact]
	public void DnaStringMissingPaddingOfTwoCharsIsSanitized() {
		var goodBase64 = Convert.ToBase64String(CreateTestDna());
		var corruptedDna = goodBase64[..^2]; // strip the two padding characters

		var output = new StringWriter();
		Console.SetOut(output);

		var portraitData = new PortraitData(corruptedDna, CreateGenesDB());

		Assert.Equal(200, portraitData.HairColorPaletteCoordinates.X);
		Assert.Single(portraitData.MorphGenesDict);
		Assert.Contains("Attempting to sanitize and recover", output.ToString());
	}

	[Fact]
	public void DnaStringMissingPaddingOfOneCharIsSanitized() {
		var goodBase64 = Convert.ToBase64String(CreateTestDna());
		var corruptedDna = goodBase64[..^1]; // strip one padding character

		var output = new StringWriter();
		Console.SetOut(output);

		var portraitData = new PortraitData(corruptedDna, CreateGenesDB());

		Assert.Equal(200, portraitData.HairColorPaletteCoordinates.X);
		Assert.Single(portraitData.MorphGenesDict);
		Assert.Contains("Attempting to sanitize and recover", output.ToString());
	}

	[Fact]
	public void HairColorXCanBeSetToZero() {
		var genesDB = new GenesDB();
		var testPortraitData = new ImperatorToCK3.Imperator.Characters.PortraitData("AAAAAAAAAAAAAAAAAH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA==", genesDB);

		Assert.Equal(0, testPortraitData.HairColorPaletteCoordinates.X);
	}
	[Fact]
	public void HairColorXCanBeSetToMax() {
		var genesDB = new GenesDB();
		var testPortraitData = new ImperatorToCK3.Imperator.Characters.PortraitData("/wAAAAAAAAAAAAAAAH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA==", genesDB);

		Assert.Equal(510, testPortraitData.HairColorPaletteCoordinates.X);
	}

	[Fact]
	public void HairColorXCanBeSetToArbitraryValue() {
		var genesDB = new GenesDB();
		var testPortraitData = new ImperatorToCK3.Imperator.Characters.PortraitData("ZAAAAAAAAAAAAAAAAH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA==", genesDB);

		Assert.Equal(200, testPortraitData.HairColorPaletteCoordinates.X);
	}

	[Fact]
	public void HairColorYCanBeSetToZero() {
		var genesDB = new GenesDB();
		var testPortraitData = new ImperatorToCK3.Imperator.Characters.PortraitData("AAAAAAAAAAAAAAAAAH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA==", genesDB);

		Assert.Equal(0, testPortraitData.HairColorPaletteCoordinates.Y);
	}

	[Fact]
	public void HairColorYCanBeSetToMax() {
		var genesDB = new GenesDB();
		var testPortraitData = new ImperatorToCK3.Imperator.Characters.PortraitData("AP8AAAAAAAAAAAAAAH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA==", genesDB);

		Assert.Equal(510, testPortraitData.HairColorPaletteCoordinates.Y);
	}

	[Fact]
	public void HairColorYCanBeSetToArbitraryValue() {
		var genesDB = new GenesDB();
		var testPortraitData = new ImperatorToCK3.Imperator.Characters.PortraitData("AGQAAAAAAAAAAAAAAH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA==", genesDB);

		Assert.Equal(200, testPortraitData.HairColorPaletteCoordinates.Y);
	}

	[Fact]
	public void SkinColorXCanBeSetToZero() {
		var genesDB = new GenesDB();
		var testPortraitData = new ImperatorToCK3.Imperator.Characters.PortraitData("AAAAAAAAAAAAAAAAAH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA==", genesDB);

		Assert.Equal(0, testPortraitData.SkinColorPaletteCoordinates.X);
	}

	[Fact]
	public void SkinColorXCanBeSetToMax() {
		var genesDB = new GenesDB();
		var testPortraitData = new ImperatorToCK3.Imperator.Characters.PortraitData("AAAAAP8AAAAAAAAAAH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA==", genesDB);

		Assert.Equal(510, testPortraitData.SkinColorPaletteCoordinates.X);
	}

	[Fact]
	public void SkinColorXCanBeSetToArbitraryValue() {
		var genesDB = new GenesDB();
		var testPortraitData = new ImperatorToCK3.Imperator.Characters.PortraitData("AAAAAGQAAAAAAAAAAH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA==", genesDB);

		Assert.Equal(200, testPortraitData.SkinColorPaletteCoordinates.X);
	}

	[Fact]
	public void SkinColorYCanBeSetToZero() {
		var genesDB = new GenesDB();
		var testPortraitData = new ImperatorToCK3.Imperator.Characters.PortraitData("AAAAAAAAAAAAAAAAAH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA==", genesDB);

		Assert.Equal(0, testPortraitData.SkinColorPaletteCoordinates.Y);
	}

	[Fact]
	public void SkinColorYCanBeSetToMax() {
		var genesDB = new GenesDB();
		var testPortraitData = new ImperatorToCK3.Imperator.Characters.PortraitData("AAAAAAD/AAAAAAAAAH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA==", genesDB);

		Assert.Equal(510, testPortraitData.SkinColorPaletteCoordinates.Y);
	}

	[Fact]
	public void SkinColorYCanBeSetToArbitraryValue() {
		var genesDB = new GenesDB();
		var testPortraitData = new ImperatorToCK3.Imperator.Characters.PortraitData("AAAAAABkAAAAAAAAAH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA==", genesDB);

		Assert.Equal(200, testPortraitData.SkinColorPaletteCoordinates.Y);
	}

	[Fact]
	public void EyeColorXCanBeSetToZero() {
		var genesDB = new GenesDB();
		var testPortraitData = new ImperatorToCK3.Imperator.Characters.PortraitData("AAAAAAAAAAAAAAAAAH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA==", genesDB);

		Assert.Equal(0, testPortraitData.EyeColorPaletteCoordinates.X);
	}

	[Fact]
	public void EyeColorXCanBeSetToMax() {
		var genesDB = new GenesDB();
		var testPortraitData = new ImperatorToCK3.Imperator.Characters.PortraitData("AAAAAAAAAAD/AAAAAH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA==", genesDB);

		Assert.Equal(510, testPortraitData.EyeColorPaletteCoordinates.X);
	}

	[Fact]
	public void EyeColorXCanBeSetToArbitraryValue() {
		var genesDB = new GenesDB();
		var testPortraitData = new ImperatorToCK3.Imperator.Characters.PortraitData("AAAAAAAAAABkAAAAAH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA==", genesDB);

		Assert.Equal(200, testPortraitData.EyeColorPaletteCoordinates.X);
	}

	[Fact]
	public void EyeColorYCanBeSetToZero() {
		var genesDB = new GenesDB();
		var testPortraitData = new ImperatorToCK3.Imperator.Characters.PortraitData("AAAAAAAAAAAAAAAAAH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA==", genesDB);

		Assert.Equal(0, testPortraitData.EyeColorPaletteCoordinates.Y);
	}

	[Fact]
	public void EyeColorYCanBeSetToMax() {
		var genesDB = new GenesDB();
		var testPortraitData = new ImperatorToCK3.Imperator.Characters.PortraitData("AAAAAAAAAAAA/wAAAH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA==", genesDB);

		Assert.Equal(510, testPortraitData.EyeColorPaletteCoordinates.Y);
	}

	[Fact]
	public void EyeColorYCanBeSetToArbitraryValue() {
		var genesDB = new GenesDB();
		var testPortraitData = new ImperatorToCK3.Imperator.Characters.PortraitData("AAAAAAAAAAAAZAAAAH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA==", genesDB);

		Assert.Equal(200, testPortraitData.EyeColorPaletteCoordinates.Y);
	}

	[Fact]
	public void ShortDna_DoesNotThrowAndKeepsDefaults() {
		var genesDB = CreateGenesDB();
		var shortDnaBytes = new byte[5];
		shortDnaBytes[0] = 10;
		shortDnaBytes[1] = 20;
		shortDnaBytes[2] = 30;
		shortDnaBytes[3] = 40;
		shortDnaBytes[4] = 50;
		string shortDna = Convert.ToBase64String(shortDnaBytes);

		var exception = Record.Exception(() => new PortraitData(shortDna, genesDB));
		Assert.Null(exception);

		var data = new PortraitData(shortDna, genesDB);
		// DNA too short for eye palette (needs 12 bytes) and for gene indices, so coords stay at defaults that were set from first 5 bytes where applicable.
		// Hair palette (0-3) should be set, skin (4-7) partially, eye (8+) should warn and keep defaults.
		// After fix, Hair should be derived from bytes 0-3, Skin should warn, Eye should warn.
		Assert.Equal(20, data.HairColorPaletteCoordinates.X); // 10*2
		Assert.Equal(40, data.HairColorPaletteCoordinates.Y); // 20*2
		// Eye should remain default 256 because length <= 11
		Assert.Equal(256, data.EyeColorPaletteCoordinates.X);
		Assert.Empty(data.AccessoryGenesDict);
		Assert.Empty(data.MorphGenesDict);
	}

	[Fact]
	public void VeryShortDna_TwoBytes_DoesNotThrow() {
		var genesDB = CreateGenesDB();
		var tinyDna = Convert.ToBase64String(new byte[2] { 1, 2 });
		var exception = Record.Exception(() => new PortraitData(tinyDna, genesDB));
		Assert.Null(exception);
		var data = new PortraitData(tinyDna, genesDB);
		// Even hair palette needs 4 bytes, so should keep defaults
		Assert.Equal(256, data.HairColorPaletteCoordinates.X);
	}

	[Fact]
	public void TwelveByteDna_SetsEyePaletteWithoutThrow() {
		var genesDB = CreateGenesDB();
		var dna = new byte[12];
		for (int i = 0; i < 12; ++i) {
			dna[i] = (byte)(i + 50);
		}
		string b64 = Convert.ToBase64String(dna);
		var exception = Record.Exception(() => new PortraitData(b64, genesDB));
		Assert.Null(exception);
		var data = new PortraitData(b64, genesDB);
		Assert.Equal(100, data.HairColorPaletteCoordinates.X); // 50*2
		Assert.Equal(116, data.EyeColorPaletteCoordinates.X); // dna[8]=58*2=116
	}
}