using commonItems;
using System;
using System.Collections.Generic;

namespace ImperatorToCK3.Imperator.Characters {
	public class PortraitData {
		private readonly Genes.GenesDB genes = new();
		public PaletteCoordinates HairColorPaletteCoordinates { get; private set; } = new();
		public PaletteCoordinates SkinColorPaletteCoordinates { get; private set; } = new();
		public PaletteCoordinates EyeColorPaletteCoordinates { get; private set; } = new();
		public List<AccessoryGeneData> AccessoryGenesList { get; private set; } = new();

		public PortraitData() { }
		public PortraitData(string dnaString, Genes.GenesDB genesDB, string ageSexString = "male") {
			genes = genesDB;
			var decodedDnaStr = Convert.FromBase64String(dnaString);
			const int hairColorPaletteXIndex = 0;
			const int hairColorPaletteYIndex = 1;
			const int skinColorPaletteXIndex = 4;
			const int skinColorPaletteYIndex = 5;
			const int eyeColorPaletteXIndex = 8;
			const int eyeColorPaletteYIndex = 9;

			// hair
			HairColorPaletteCoordinates.X = (uint)decodedDnaStr[hairColorPaletteXIndex] * 2;
			HairColorPaletteCoordinates.Y = (uint)decodedDnaStr[hairColorPaletteYIndex] * 2;
			// skin
			SkinColorPaletteCoordinates.X = (uint)decodedDnaStr[skinColorPaletteXIndex] * 2;
			SkinColorPaletteCoordinates.Y = (uint)decodedDnaStr[skinColorPaletteYIndex] * 2;
			// eyes
			EyeColorPaletteCoordinates.X = (uint)decodedDnaStr[eyeColorPaletteXIndex] * 2;
			EyeColorPaletteCoordinates.Y = (uint)decodedDnaStr[eyeColorPaletteYIndex] * 2;

			// accessory genes
			const uint colorGenesBytes = 12;
			var accessoryGenes = genes.Genes.Genes;
			var accessoryGenesIndex = genes.Genes.Index;
			foreach (var (geneName, gene) in accessoryGenes) {
				var geneIndex = gene.Index;

				var geneTemplateByteIndex = colorGenesBytes + ((accessoryGenesIndex + geneIndex - 3) * 4);
				var characterGeneTemplateIndex = (uint)decodedDnaStr[geneTemplateByteIndex];
				var geneTemplateName = gene.GetGeneTemplateByIndex(characterGeneTemplateIndex).Key;

				var geneTemplateObjectByteIndex = colorGenesBytes + ((accessoryGenesIndex + geneIndex - 3) * 4) + 1;
				var characterGeneSliderValue = (uint)decodedDnaStr[geneTemplateObjectByteIndex] / 255;

				if (gene.GeneTemplates[geneTemplateName].AgeSexWeightBlocks.TryGetValue(ageSexString, out var characterGeneFoundWeightBlock)) {
					var characterGeneObjectName = characterGeneFoundWeightBlock.GetMatchingObject(characterGeneSliderValue);
					if (characterGeneObjectName is not null) {
						AccessoryGenesList.Add(new AccessoryGeneData { geneName = geneName, geneTemplate = geneTemplateName, objectName = characterGeneObjectName });
					} else {
						Logger.Warn($"\t\t\tgene template object name {geneTemplateName} for {ageSexString} could not be extracted from DNA.");
					}
				}
			}
		}
	}
}
