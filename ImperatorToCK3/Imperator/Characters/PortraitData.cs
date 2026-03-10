using commonItems;
using ImperatorToCK3.CommonUtils.Genes;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using ZLinq;

namespace ImperatorToCK3.Imperator.Characters; 

internal sealed class PortraitData {
	public PaletteCoordinates HairColorPaletteCoordinates { get; } = new();
	public PaletteCoordinates HairColor2PaletteCoordinates { get; } = new();
	public PaletteCoordinates SkinColorPaletteCoordinates { get; } = new();
	public PaletteCoordinates SkinColor2PaletteCoordinates { get; } = new();
	public PaletteCoordinates EyeColorPaletteCoordinates { get; } = new();
	public PaletteCoordinates EyeColor2PaletteCoordinates { get; } = new();

	public Dictionary<string, AccessoryGeneData> AccessoryGenesDict { get; } = [];
	public Dictionary<string, MorphGeneData> MorphGenesDict { get; } = [];
	
	private static readonly FrozenSet<string> morphGenesToIgnore = ["expression"];

	public PortraitData(string dnaString, GenesDB genesDB, string ageSexString = "male") {
		var decodedDnaStr = Convert.FromBase64String(dnaString);
		SetHairColorPaletteCoordinates(decodedDnaStr);
		SetSkinColorPaletteCoordinates(decodedDnaStr);
		SetEyeColorPaletteCoordinates(decodedDnaStr);

		ProcessMorphGeneData(genesDB, decodedDnaStr);

		ProcessAccessoryGeneData(genesDB, ageSexString, decodedDnaStr);
	}

	private void ProcessAccessoryGeneData(GenesDB genesDB, string ageSexString, byte[] decodedDnaStr) {
		// accessory genes
		foreach (var gene in genesDB.AccessoryGenes) {
			var geneIndex = gene.Index;
			if (geneIndex is null) {
				continue;
			}

			var geneTemplateByteIndex = geneIndex.Value * 4;
			if (decodedDnaStr.Length <= geneTemplateByteIndex + 3) {
				Logger.Warn($"DNA string is too short for gene {gene.Id}!");
				continue;
			}
			var geneTemplateIndex = (uint)decodedDnaStr[geneTemplateByteIndex];
			var geneTemplateRecessiveIndex = (uint)decodedDnaStr[geneTemplateByteIndex + 2];
			var geneTemplateName = gene.GetGeneTemplateByIndex(geneTemplateIndex).Id;
			var geneTemplateNameRecessive = gene.GetGeneTemplateByIndex(geneTemplateRecessiveIndex).Id;

			var geneTemplateObjectByteIndex = geneTemplateByteIndex + 1;
			var geneTemplateObjectRecessiveByteIndex = geneTemplateByteIndex + 3;
			var geneSliderPercentage = (double)decodedDnaStr[geneTemplateObjectByteIndex] / 255;
			var geneSliderRecessivePercentage = (double)decodedDnaStr[geneTemplateObjectRecessiveByteIndex] / 255;

			if (!gene.GeneTemplates[geneTemplateName].AgeSexWeightBlocks.TryGetValue(ageSexString, out var foundWeightBlock)) {
				continue;
			}
			if (!gene.GeneTemplates[geneTemplateNameRecessive].AgeSexWeightBlocks.TryGetValue(ageSexString, out var foundWeightBlockRecessive)) {
				continue;
			}

			var geneObjectName = foundWeightBlock.GetMatchingObject(geneSliderPercentage);
			var geneObjectNameRecessive = foundWeightBlockRecessive.GetMatchingObject(geneSliderRecessivePercentage);
			if (geneObjectName is not null && geneObjectNameRecessive is not null) {
				AccessoryGenesDict.Add(gene.Id, new AccessoryGeneData {
					GeneTemplate = geneTemplateName,
					ObjectName = geneObjectName,
					GeneTemplateRecessive = geneTemplateNameRecessive,
					ObjectNameRecessive = geneObjectNameRecessive
				});
			} else {
				Logger.Warn($"{ageSexString} gene template object name for {geneTemplateName} for {gene.Id} could not be extracted from DNA!");
			}
		}
	}

	private void ProcessMorphGeneData(GenesDB genesDB, byte[] decodedDnaStr) {
		// morph genes
		var morphGenesToLoad = genesDB.MorphGenes.AsValueEnumerable()
			.Where(g => !morphGenesToIgnore.Contains(g.Id));
		foreach (var gene in morphGenesToLoad) {
			var geneIndex = gene.Index;
			if (geneIndex is null) {
				continue;
			}

			var geneTemplateByteIndex = geneIndex.Value * 4;
			if (decodedDnaStr.Length <= geneTemplateByteIndex + 3) {
				Logger.Warn($"DNA string is too short for gene {gene.Id}!");
				continue;
			}
			var geneTemplateIndex = (uint)decodedDnaStr[geneTemplateByteIndex];
			var geneTemplateRecessiveIndex = (uint)decodedDnaStr[geneTemplateByteIndex + 2];
			var geneTemplateName = gene.GetGeneTemplateByIndex(geneTemplateIndex)?.Id;
			if (geneTemplateName is null) {
				continue;
			}
			var geneTemplateRecessiveName = gene.GetGeneTemplateByIndex(geneTemplateRecessiveIndex)?.Id;
			if (geneTemplateRecessiveName is null) {
				continue;
			}

			var geneTemplateValueByteIndex = geneTemplateByteIndex + 1;
			var geneTemplateValueRecessiveByteIndex = geneTemplateByteIndex + 3;
			// Get gene value (0-255).
			var geneValue = decodedDnaStr[geneTemplateValueByteIndex];
			var geneValueRecessive = decodedDnaStr[geneTemplateValueRecessiveByteIndex];
			MorphGenesDict.Add(gene.Id, new MorphGeneData {
				TemplateName = geneTemplateName,
				Value = geneValue,
				TemplateRecessiveName = geneTemplateRecessiveName,
				ValueRecessive = geneValueRecessive
			});
		}
	}

	private void SetEyeColorPaletteCoordinates(byte[] decodedDnaStr) {
		const int eyeColorPaletteXIndex = 8;

		EyeColorPaletteCoordinates.X = decodedDnaStr[eyeColorPaletteXIndex] * 2;
		EyeColorPaletteCoordinates.Y = decodedDnaStr[eyeColorPaletteXIndex + 1] * 2;
		EyeColor2PaletteCoordinates.X = decodedDnaStr[eyeColorPaletteXIndex + 2] * 2;
		EyeColor2PaletteCoordinates.Y = decodedDnaStr[eyeColorPaletteXIndex + 3] * 2;
	}

	private void SetSkinColorPaletteCoordinates(byte[] decodedDnaStr) {
		const int skinColorPaletteXIndex = 4;

		SkinColorPaletteCoordinates.X = decodedDnaStr[skinColorPaletteXIndex] * 2;
		SkinColorPaletteCoordinates.Y = decodedDnaStr[skinColorPaletteXIndex + 1] * 2;
		SkinColor2PaletteCoordinates.X = decodedDnaStr[skinColorPaletteXIndex + 2] * 2;
		SkinColor2PaletteCoordinates.Y = decodedDnaStr[skinColorPaletteXIndex + 3] * 2;
	}

	private void SetHairColorPaletteCoordinates(byte[] decodedDnaStr) {
		const int hairColorPaletteXIndex = 0;

		HairColorPaletteCoordinates.X = decodedDnaStr[hairColorPaletteXIndex] * 2;
		HairColorPaletteCoordinates.Y = decodedDnaStr[hairColorPaletteXIndex + 1] * 2;
		HairColor2PaletteCoordinates.X = decodedDnaStr[hairColorPaletteXIndex + 2] * 2;
		HairColor2PaletteCoordinates.Y = decodedDnaStr[hairColorPaletteXIndex + 3] * 2;
	}
}