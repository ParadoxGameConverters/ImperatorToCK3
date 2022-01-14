using commonItems;
using ImperatorToCK3.CommonUtils.Genes;
using System;
using System.Collections.Generic;

namespace ImperatorToCK3.Imperator.Characters {
	public class PortraitData {
		private readonly GenesDB genes = new();
		public PaletteCoordinates HairColorPaletteCoordinates { get; private set; } = new();
		public PaletteCoordinates HairColor2PaletteCoordinates { get; private set; } = new();
		public PaletteCoordinates SkinColorPaletteCoordinates { get; private set; } = new();
		public PaletteCoordinates SkinColor2PaletteCoordinates { get; private set; } = new();
		public PaletteCoordinates EyeColorPaletteCoordinates { get; private set; } = new();
		public PaletteCoordinates EyeColor2PaletteCoordinates { get; private set; } = new();
		public Dictionary<string, AccessoryGeneData> AccessoryGenesDict { get; private set; } = new();

		public PortraitData() { }
		public PortraitData(string dnaString, GenesDB genesDB, string ageSexString = "male") {
			genes = genesDB;
			var decodedDnaStr = Convert.FromBase64String(dnaString);
			const int hairColorPaletteXIndex = 0;
			const int skinColorPaletteXIndex = 4;
			const int eyeColorPaletteXIndex = 8;

			// hair
			HairColorPaletteCoordinates.X = decodedDnaStr[hairColorPaletteXIndex] * 2;
			HairColorPaletteCoordinates.Y = decodedDnaStr[hairColorPaletteXIndex + 1] * 2;
			HairColor2PaletteCoordinates.X = decodedDnaStr[hairColorPaletteXIndex + 2] * 2;
			HairColor2PaletteCoordinates.Y = decodedDnaStr[hairColorPaletteXIndex + 3] * 2;
			// skin
			SkinColorPaletteCoordinates.X = decodedDnaStr[skinColorPaletteXIndex] * 2;
			SkinColorPaletteCoordinates.Y = decodedDnaStr[skinColorPaletteXIndex + 1] * 2;
			SkinColor2PaletteCoordinates.X = decodedDnaStr[skinColorPaletteXIndex + 2] * 2;
			SkinColor2PaletteCoordinates.Y = decodedDnaStr[skinColorPaletteXIndex + 3] * 2;
			// eyes
			EyeColorPaletteCoordinates.X = decodedDnaStr[eyeColorPaletteXIndex] * 2;
			EyeColorPaletteCoordinates.Y = decodedDnaStr[eyeColorPaletteXIndex + 1] * 2;
			EyeColor2PaletteCoordinates.X = decodedDnaStr[eyeColorPaletteXIndex + 2] * 2;
			EyeColor2PaletteCoordinates.Y = decodedDnaStr[eyeColorPaletteXIndex + 3] * 2;

			// accessory genes
			const uint colorGenesBytes = 12;
			var accessoryGenes = genes.Genes;

			foreach (var (geneName, gene) in accessoryGenes) {
				var geneIndex = gene.Index;
				if (geneIndex is null) {
					continue;
				}

				var geneTemplateByteIndex = colorGenesBytes + (((uint)geneIndex - 3) * 4);
				var characterGeneTemplateIndex = (uint)decodedDnaStr[geneTemplateByteIndex];
				var geneTemplateName = gene.GetGeneTemplateByIndex(characterGeneTemplateIndex).Key;

				var geneTemplateObjectByteIndex = colorGenesBytes + (((uint)geneIndex - 3) * 4) + 1;
				var characterGeneSliderPercentage = (double)decodedDnaStr[geneTemplateObjectByteIndex] / 255;

				if (gene.GeneTemplates[geneTemplateName].AgeSexWeightBlocks.TryGetValue(ageSexString, out var characterGeneFoundWeightBlock)) {
					var characterGeneObjectName = characterGeneFoundWeightBlock.GetMatchingObject(characterGeneSliderPercentage);
					if (characterGeneObjectName is not null) {
						AccessoryGenesDict.Add(geneName, new AccessoryGeneData() { geneTemplate = geneTemplateName, objectName = characterGeneObjectName });
					} else {
						Logger.Warn($"\t\t\tGene template object name {geneTemplateName} for {ageSexString} could not be extracted from DNA!");
					}
				}
			}
		}
	}
}
