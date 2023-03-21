using commonItems;
using commonItems.Mods;
using ImageMagick;
using ImperatorToCK3.CommonUtils.Genes;
using ImperatorToCK3.Exceptions;
using ImperatorToCK3.Imperator.Characters;
using ImperatorToCK3.Mappers.Gene;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace ImperatorToCK3.CK3.Characters; 

public sealed class DNAFactory {
	private readonly IUnsafePixelCollection<ushort> irHairPalettePixels;
	private readonly IUnsafePixelCollection<ushort> irSkinPalettePixels;
	private readonly IUnsafePixelCollection<ushort> irEyePalettePixels;
	
	private readonly Dictionary<IMagickColor<ushort>, DNA.PaletteCoordinates> ck3HairColorToPaletteCoordinatesDict = new();
	private readonly Dictionary<IMagickColor<ushort>, DNA.PaletteCoordinates> ck3SkinColorToPaletteCoordinatesDict = new();
	private readonly Dictionary<IMagickColor<ushort>, DNA.PaletteCoordinates> ck3EyeColorToPaletteCoordinatesDict = new();
	
	private readonly GenesDB genesDB;
	private readonly AccessoryGeneMapper accessoryGeneMapper = new("configurables/accessory_genes_map.txt");

	public DNAFactory(ModFilesystem irModFS, ModFilesystem ck3ModFS) {
		var irHairPalettePath = irModFS.GetActualFileLocation("gfx/portraits/hair_palette.dds") ??
		                        throw new ConverterException("Could not find Imperator hair palette!");
		irHairPalettePixels = new MagickImage(irHairPalettePath).GetPixelsUnsafe();
		var ck3HairPalettePath = ck3ModFS.GetActualFileLocation("gfx/portraits/hair_palette.dds") ??
		                         throw new ConverterException("Could not find CK3 hair palette!");
		var ck3HairPalettePixels = new MagickImage(ck3HairPalettePath).GetPixelsUnsafe();

		var irSkinPalettePath = irModFS.GetActualFileLocation("gfx/portraits/skin_palette.dds") ??
		                        throw new ConverterException("Could not find Imperator skin palette!");
		irSkinPalettePixels = new MagickImage(irSkinPalettePath).GetPixelsUnsafe();
		var ck3SkinPalettePath = ck3ModFS.GetActualFileLocation("gfx/portraits/skin_palette.dds") ??
		                         throw new ConverterException("Could not find CK3 skin palette!");
		var ck3SkinPalettePixels = new MagickImage(ck3SkinPalettePath).GetPixelsUnsafe();

		var irEyePalettePath = irModFS.GetActualFileLocation("gfx/portraits/eye_palette.dds") ??
		                       throw new ConverterException("Could not find Imperator eye palette!");
		irEyePalettePixels = new MagickImage(irEyePalettePath).GetPixelsUnsafe();
		var ck3EyePalettePath = ck3ModFS.GetActualFileLocation("gfx/portraits/eye_palette.dds") ??
		                        throw new ConverterException("Could not find CK3 eye palette!");
		var ck3EyePalettePixels = new MagickImage(ck3EyePalettePath).GetPixelsUnsafe();

		genesDB = new GenesDB(ck3ModFS);
		
		BuildColorConversionCaches(ck3HairPalettePixels, ck3SkinPalettePixels, ck3EyePalettePixels);
	}
	
	public DNA GenerateDNA(Imperator.Characters.Character irCharacter, PortraitData irPortraitData) {
		var id = $"dna_{irCharacter.Id}";
		
		var dnaValues = new Dictionary<string, string>();

		var hairCoordinates = GetPaletteCoordinates(
			irPortraitData.HairColorPaletteCoordinates, irHairPalettePixels, ck3HairColorToPaletteCoordinatesDict
		);
		var hairCoordinates2 = GetPaletteCoordinates(
			irPortraitData.HairColor2PaletteCoordinates, irHairPalettePixels, ck3HairColorToPaletteCoordinatesDict
		);
		var hairValue = $"{hairCoordinates.X} {hairCoordinates.Y} {hairCoordinates2.X} {hairCoordinates2.Y}";
		dnaValues.Add("hair_color", hairValue);

		var skinCoordinates = GetPaletteCoordinates(
			irPortraitData.SkinColorPaletteCoordinates, irSkinPalettePixels, ck3SkinColorToPaletteCoordinatesDict
		);
		var skinCoordinates2 = GetPaletteCoordinates(
			irPortraitData.SkinColor2PaletteCoordinates, irSkinPalettePixels, ck3SkinColorToPaletteCoordinatesDict
		);
		var skinValue = $"{skinCoordinates.X} {skinCoordinates.Y} {skinCoordinates2.X} {skinCoordinates2.Y}";
		dnaValues.Add("skin_color", skinValue);

		var eyeCoordinates = GetPaletteCoordinates(
			irPortraitData.EyeColorPaletteCoordinates, irEyePalettePixels, ck3EyeColorToPaletteCoordinatesDict
		);
		var eyeCoordinates2 = GetPaletteCoordinates(
			irPortraitData.EyeColor2PaletteCoordinates, irEyePalettePixels, ck3EyeColorToPaletteCoordinatesDict
		);
		var eyeValue = $"{eyeCoordinates.X} {eyeCoordinates.Y} {eyeCoordinates2.X} {eyeCoordinates2.Y}";
		dnaValues.Add("eye_color", eyeValue);
		
		var accessoryGeneValue = GetAccessoryGeneValue(
			irCharacter, 
			irPortraitData, 
			"beards", 
			"beards", 
			"scripted_character_beards_01"
		);
		dnaValues.Add("beards", accessoryGeneValue);

		// Use middle values for the rest of the genes.
		var missingMorphGenes = genesDB!.MorphGenes.Where(g => !dnaValues.ContainsKey(g.Key));
		foreach (var (geneName, gene) in missingMorphGenes) {
			var geneTemplates = gene.GeneTemplates
				.OrderBy(t => t.Index)
				.ToImmutableList();
			var visibleGeneTemplates = geneTemplates
				.Where(t => t.Visible)
				.ToImmutableList();
			var geneTemplatesToUse = visibleGeneTemplates.Count > 0 ? visibleGeneTemplates : geneTemplates;
			// Get middle gene template.
			var templateName = geneTemplatesToUse.ElementAt(geneTemplatesToUse.Count / 2).Id;
			var geneValue = $"\"{templateName}\" 128 \"{templateName}\" 128";
			dnaValues.Add(geneName, geneValue);
		}

		var accessoryGenesToIgnore = new[] {
			"props", "props_2", "special_legwear", "cloaks",
			"special_headgear_head_bandage", "special_headgear_eye_patch", "special_headgear_face_mask",
			"special_headgear_blindfold", "special_headgear_spectacles"
		};
		var missingAccessoryGenes = genesDB.AccessoryGenes
			.Where(g => !dnaValues.ContainsKey(g.Key))
			.Where(g => !accessoryGenesToIgnore.Contains(g.Key));
		foreach (var (geneName, gene) in missingAccessoryGenes) {
			var geneTemplates = gene.GeneTemplates
				.OrderBy(t => t.Index)
				.ToImmutableList();
			// Get middle gene template.
			var templateName = geneTemplates.ElementAt(geneTemplates.Count / 2).Id;
			var geneValue = $"\"{templateName}\" 128 \"{templateName}\" 128";
			dnaValues.Add(geneName, geneValue);
		}
		
		return new DNA(id, dnaValues);
	}

	private string GetAccessoryGeneValue(
		Imperator.Characters.Character irCharacter,
		PortraitData irPortraitData,
		string imperatorGeneName,
		string ck3GeneName,
		string ck3GeneSetName
	) {
		var geneInfo = irPortraitData.AccessoryGenesDict[imperatorGeneName];
		var geneSet = genesDB.AccessoryGenes[ck3GeneName].GeneTemplates[ck3GeneSetName];

		var mappings = accessoryGeneMapper.Mappings[imperatorGeneName];
		var convertedSetEntry = mappings[geneInfo.ObjectName];
		var convertedSetEntryRecessive = mappings[geneInfo.ObjectNameRecessive];

		var matchingPercentage = geneSet.AgeSexWeightBlocks[irCharacter.AgeSex].GetMatchingPercentage(convertedSetEntry);
		var matchingPercentageRecessive = geneSet.AgeSexWeightBlocks[irCharacter.AgeSex].GetMatchingPercentage(convertedSetEntryRecessive);
		int intSliderValue = (int)Math.Ceiling(matchingPercentage * 255);
		int intSliderValueRecessive = (int)Math.Ceiling(matchingPercentageRecessive * 255);

		var geneValue = $"\"{ck3GeneSetName}\" {intSliderValue} \"{ck3GeneSetName}\" {intSliderValueRecessive}";
		return geneValue;
	}

	private void BuildColorConversionCaches(
		IUnsafePixelCollection<ushort> ck3HairPalettePixels,
		IUnsafePixelCollection<ushort> ck3SkinPalettePixels,
		IUnsafePixelCollection<ushort> ck3EyePalettePixels
	) {
		BuildColorConversionCache(ck3HairPalettePixels, ck3HairColorToPaletteCoordinatesDict);
		BuildColorConversionCache(ck3SkinPalettePixels, ck3SkinColorToPaletteCoordinatesDict);
		BuildColorConversionCache(ck3EyePalettePixels, ck3EyeColorToPaletteCoordinatesDict);
	}

	private static void BuildColorConversionCache(
		IUnsafePixelCollection<ushort> ck3PalettePixels,
		IDictionary<IMagickColor<ushort>, DNA.PaletteCoordinates> ck3ColorToCoordinatesDict
	) {
		foreach (var pixel in ck3PalettePixels) {
			var color = pixel.ToColor();
			if (color is null) {
				continue;
			}

			var coordinates = new DNA.PaletteCoordinates { X = pixel.X, Y = pixel.Y };
			ck3ColorToCoordinatesDict[color] = coordinates;
		}
	}

	private static DNA.PaletteCoordinates GetPaletteCoordinates(
		PaletteCoordinates irPaletteCoordinates,
		IUnsafePixelCollection<ushort> irPalettePixels,
		IDictionary<IMagickColor<ushort>, DNA.PaletteCoordinates> ck3ColorToCoordinatesDict
	) {
		var irColor = irPalettePixels.GetPixel(irPaletteCoordinates.X, irPaletteCoordinates.Y).ToColor();
		if (irColor is null) {
			Logger.Warn($"Cannot get color from palette {irPalettePixels}!");
			return new DNA.PaletteCoordinates();
		}
		
		if (ck3ColorToCoordinatesDict.TryGetValue(irColor, out var foundCoordinates)) {
			return foundCoordinates;
		}

		// Find the closest color in the CK3 palette with the 3D distance formula.
		var closestPair = ck3ColorToCoordinatesDict
			.MinBy(d => Math.Pow(d.Key.R - irColor.R, 2) + Math.Pow(d.Key.G - irColor.G, 2) + Math.Pow(d.Key.B - irColor.B, 2));
		var closestColorCoordinates = closestPair.Value;
		ck3ColorToCoordinatesDict[irColor] = closestColorCoordinates;
		return closestColorCoordinates;
	}
}