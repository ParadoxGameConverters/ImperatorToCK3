using System;
using System.Collections.Generic;
using commonItems;

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

			// hair
			HairColorPaletteCoordinates.x = (uint)decodedDnaStr[0] * 2;
			HairColorPaletteCoordinates.y = (uint)decodedDnaStr[1] * 2;
			// skin
			SkinColorPaletteCoordinates.x = (uint)decodedDnaStr[4] * 2;
			SkinColorPaletteCoordinates.y = (uint)decodedDnaStr[5] * 2;
			// eyes
			EyeColorPaletteCoordinates.x = (uint)decodedDnaStr[8] * 2;
			EyeColorPaletteCoordinates.y = (uint)decodedDnaStr[9] * 2;

			// accessory genes
			const uint colorGenesBytes = 12;
			var accessoryGenes = genes.Genes.Genes;
			//Logger.Debug($"ageSex: {ageSexString}");
			var accessoryGenesIndex = genes.Genes.Index;
			foreach (var (geneName, gene) in accessoryGenes) {
				var geneIndex = gene.Index;
				//Logger.Debug("\tgene: " + geneName);

				var geneTemplateByteIndex = colorGenesBytes + ((accessoryGenesIndex + geneIndex - 3) * 4);
				var characterGeneTemplateIndex = (uint)decodedDnaStr[geneTemplateByteIndex];
				var geneTemplateName = gene.GetGeneTemplateByIndex(characterGeneTemplateIndex).Key;
				//Logger.Debug("\t\tgene template: " + fst);

				var geneTemplateObjectByteIndex = colorGenesBytes + (accessoryGenesIndex + geneIndex - 3) * 4 + 1;
				var characterGeneSliderValue = (uint)decodedDnaStr[geneTemplateObjectByteIndex] / 255;

				if (gene.GeneTemplates[geneTemplateName].AgeSexWeightBlocks.TryGetValue(ageSexString, out var characterGeneFoundWeightBlock)) {
					var characterGeneObjectName = characterGeneFoundWeightBlock.GetMatchingObject(characterGeneSliderValue);
					if (characterGeneObjectName is not null) {
						//Logger.Debug("\t\tgene template object: " + characterGeneObjectName);
						AccessoryGenesList.Add(new AccessoryGeneData() { geneName = geneName, geneTemplate = geneTemplateName, objectName = characterGeneObjectName });
						//Logger.Debug("\t\tStruct size: " + accessoryGenesVector.Count);
					}
				} else {
					Logger.Warn("\t\t\tgene template object name could not be extracted from DNA.");
				}
			}
		}
	}
}
