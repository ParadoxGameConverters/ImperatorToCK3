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
			HairColorPaletteCoordinates.x = decodedDnaStr[hairColorPaletteXIndex] * 2;
			HairColorPaletteCoordinates.y = decodedDnaStr[hairColorPaletteXIndex + 1] * 2;
			HairColor2PaletteCoordinates.x = decodedDnaStr[hairColorPaletteXIndex + 2] * 2;
			HairColor2PaletteCoordinates.y = decodedDnaStr[hairColorPaletteXIndex + 3] * 2;
			// skin
			SkinColorPaletteCoordinates.x = decodedDnaStr[skinColorPaletteXIndex] * 2;
			SkinColorPaletteCoordinates.y = decodedDnaStr[skinColorPaletteXIndex + 1] * 2;
			SkinColor2PaletteCoordinates.x = decodedDnaStr[skinColorPaletteXIndex + 2] * 2;
			SkinColor2PaletteCoordinates.y = decodedDnaStr[skinColorPaletteXIndex + 3] * 2;
			// eyes
			EyeColorPaletteCoordinates.x = decodedDnaStr[eyeColorPaletteXIndex] * 2;
			EyeColorPaletteCoordinates.y = decodedDnaStr[eyeColorPaletteXIndex + 1] * 2;
			EyeColor2PaletteCoordinates.x = decodedDnaStr[eyeColorPaletteXIndex + 2] * 2;
			EyeColor2PaletteCoordinates.y = decodedDnaStr[eyeColorPaletteXIndex + 3] * 2;

			// accessory genes
			const uint colorGenesBytes = 12;
			var accessoryGenes = genes.Genes.Genes;

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
