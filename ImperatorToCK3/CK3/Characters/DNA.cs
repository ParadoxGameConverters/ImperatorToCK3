﻿using commonItems;
using commonItems.Mods;
using ImageMagick;
using ImperatorToCK3.CommonUtils.Genes;
using ImperatorToCK3.Exceptions;
using ImperatorToCK3.Imperator.Characters;
using ImperatorToCK3.Mappers.Gene;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace ImperatorToCK3.CK3.Characters;

public class DNA {
	public class PaletteCoordinates {
		// hair, skin and eye color palettes are 256x256
		public int X { get; init; } = 128;
		public int Y { get; init; } = 128;
	}

	public string Id { get; }
	public PaletteCoordinates HairCoordinates { get; set; }
	public PaletteCoordinates HairCoordinates2 { get; set; }
	public PaletteCoordinates SkinCoordinates { get; set; }
	public PaletteCoordinates SkinCoordinates2 { get; set; }
	public PaletteCoordinates EyeCoordinates { get; set; }
	public PaletteCoordinates EyeCoordinates2 { get; set; }

	private static IUnsafePixelCollection<ushort>? irHairPalettePixels;
	private static IUnsafePixelCollection<ushort>? ck3HairPalettePixels;

	private static IUnsafePixelCollection<ushort>? irSkinPalettePixels;
	private static IUnsafePixelCollection<ushort>? ck3SkinPalettePixels;

	private static IUnsafePixelCollection<ushort>? irEyePalettePixels;
	private static IUnsafePixelCollection<ushort>? ck3EyePalettePixels;

	private static GenesDB? genesDB;
	private static readonly AccessoryGeneMapper accessoryGeneMapper = new("configurables/accessory_genes_map.txt");
	private readonly Dictionary<string, string> dnaValues = new();

	public IReadOnlyDictionary<string, string> DNAValues => dnaValues; // <gene name, DNA value>
	public IEnumerable<string> DNALines => dnaValues.Select(kvp => $"{kvp.Key}={{{kvp.Value}}}");

	public static void Initialize(ModFilesystem irModFS, ModFilesystem ck3ModFS) {
		var irHairPalettePath = irModFS.GetActualFileLocation("gfx/portraits/hair_palette.dds") ??
		                         throw new ConverterException("Could not find Imperator hair palette!");
		irHairPalettePixels = new MagickImage(irHairPalettePath).GetPixelsUnsafe();
		var ck3HairPalettePath = ck3ModFS.GetActualFileLocation("gfx/portraits/hair_palette.dds") ??
		                         throw new ConverterException("Could not find CK3 hair palette!");
		ck3HairPalettePixels = new MagickImage(ck3HairPalettePath).GetPixelsUnsafe();

		var irSkinPalettePath = irModFS.GetActualFileLocation("gfx/portraits/skin_palette.dds") ??
		                         throw new ConverterException("Could not find Imperator skin palette!");
		irSkinPalettePixels = new MagickImage(irSkinPalettePath).GetPixelsUnsafe();
		var ck3SkinPalettePath = ck3ModFS.GetActualFileLocation("gfx/portraits/skin_palette.dds") ??
		                         throw new ConverterException("Could not find CK3 skin palette!");
		ck3SkinPalettePixels = new MagickImage(ck3SkinPalettePath).GetPixelsUnsafe();

		var irEyePalettePath = irModFS.GetActualFileLocation("gfx/portraits/eye_palette.dds") ??
		                        throw new ConverterException("Could not find Imperator eye palette!");
		irEyePalettePixels = new MagickImage(irEyePalettePath).GetPixelsUnsafe();
		var ck3EyePalettePath = ck3ModFS.GetActualFileLocation("gfx/portraits/eye_palette.dds") ??
		                        throw new ConverterException("Could not find CK3 eye palette!");
		ck3EyePalettePixels = new MagickImage(ck3EyePalettePath).GetPixelsUnsafe();

		genesDB = new GenesDB(ck3ModFS);
	}

	public DNA(Imperator.Characters.Character irCharacter, PortraitData irPortraitData) {
		Id = $"dna_{irCharacter.Id}";

		HairCoordinates = GetPaletteCoordinates(
			irPortraitData.HairColorPaletteCoordinates, irHairPalettePixels, ck3HairColorToPaletteCoordinatesDict
		);
		HairCoordinates2 = GetPaletteCoordinates(
			irPortraitData.HairColor2PaletteCoordinates, irHairPalettePixels, ck3HairColorToPaletteCoordinatesDict
		);
		var hairValue = $"{HairCoordinates.X} {HairCoordinates.Y} {HairCoordinates2.X} {HairCoordinates2.Y}";
		dnaValues.Add("hair_color", hairValue);

		SkinCoordinates = GetPaletteCoordinates(
			irPortraitData.SkinColorPaletteCoordinates, irSkinPalettePixels, ck3SkinColorToPaletteCoordinatesDict
		);
		SkinCoordinates2 = GetPaletteCoordinates(
			irPortraitData.SkinColor2PaletteCoordinates, irSkinPalettePixels, ck3SkinColorToPaletteCoordinatesDict
		);
		var skinValue = $"{SkinCoordinates.X} {SkinCoordinates.Y} {SkinCoordinates2.X} {SkinCoordinates2.Y}";
		dnaValues.Add("skin_color", skinValue);

		EyeCoordinates = GetPaletteCoordinates(
			irPortraitData.EyeColorPaletteCoordinates, irEyePalettePixels, ck3EyeColorToPaletteCoordinatesDict
		);
		EyeCoordinates2 = GetPaletteCoordinates(
			irPortraitData.EyeColor2PaletteCoordinates, irEyePalettePixels, ck3EyeColorToPaletteCoordinatesDict
		);
		var eyeValue = $"{EyeCoordinates.X} {EyeCoordinates.Y} {EyeCoordinates2.X} {EyeCoordinates2.Y}";
		dnaValues.Add("eye_color", eyeValue);

		ConvertAccessoryGene(irCharacter, irPortraitData, "beards", "beards", "scripted_character_beards_01");

		// Use middle values for the rest of the genes.
		var missingMorphGenes = genesDB!.MorphGenes.Where(g => !DNAValues.ContainsKey(g.Key));
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
			.Where(g => !DNAValues.ContainsKey(g.Key))
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
	}

	private void ConvertAccessoryGene(
		Imperator.Characters.Character irCharacter,
		PortraitData irPortraitData,
		string imperatorGeneName,
		string ck3GeneName,
		string ck3GeneSetName
	) {
		if (!irPortraitData.AccessoryGenesDict.TryGetValue(imperatorGeneName, out var geneInfo)) {
			return;
		}
		if (genesDB is null) {
			Logger.Error("Cannot determine accessory genes: genes DB is uninitialized!");
			return;
		}
		var geneSet = genesDB.AccessoryGenes[ck3GeneName].GeneTemplates[ck3GeneSetName];

		var mappings = accessoryGeneMapper.Mappings[imperatorGeneName];
		var convertedSetEntry = mappings[geneInfo.ObjectName];
		var convertedSetEntryRecessive = mappings[geneInfo.ObjectNameRecessive];

		var matchingPercentage = geneSet.AgeSexWeightBlocks[irCharacter.AgeSex].GetMatchingPercentage(convertedSetEntry);
		var matchingPercentageRecessive = geneSet.AgeSexWeightBlocks[irCharacter.AgeSex].GetMatchingPercentage(convertedSetEntryRecessive);
		int intSliderValue = (int)Math.Ceiling(matchingPercentage * 255);
		int intSliderValueRecessive = (int)Math.Ceiling(matchingPercentageRecessive * 255);

		var geneValue = $"\"{ck3GeneSetName}\" {intSliderValue} \"{ck3GeneSetName}\" {intSliderValueRecessive}";
		dnaValues.Add(ck3GeneName, geneValue);
	}
	
	private static readonly Dictionary<IMagickColor<ushort>, PaletteCoordinates> ck3HairColorToPaletteCoordinatesDict = new();
	private static readonly Dictionary<IMagickColor<ushort>, PaletteCoordinates> ck3EyeColorToPaletteCoordinatesDict = new();
	private static readonly Dictionary<IMagickColor<ushort>, PaletteCoordinates> ck3SkinColorToPaletteCoordinatesDict = new();
	public static void BuildColorConversionCaches() { // TODO: call this somewhere
		BuildColorConversionCache(ck3HairPalettePixels, ck3SkinColorToPaletteCoordinatesDict);
		BuildColorConversionCache(ck3EyePalettePixels, ck3EyeColorToPaletteCoordinatesDict);
		BuildColorConversionCache(ck3SkinPalettePixels, ck3SkinColorToPaletteCoordinatesDict);
	}

	private static void BuildColorConversionCache(
		IUnsafePixelCollection<ushort> ck3PalettePixels,
		IDictionary<IMagickColor<ushort>, PaletteCoordinates> ck3ColorToCoordinatesDict
	) {
		foreach (var pixel in ck3PalettePixels) {
			var color = pixel.ToColor();
			if (color is null) {
				continue;
			}

			var coordinates = new PaletteCoordinates { X = pixel.X, Y = pixel.Y };
			ck3ColorToCoordinatesDict[color] = coordinates;
		}
	}

	private static PaletteCoordinates GetPaletteCoordinates(
		Imperator.Characters.PaletteCoordinates irPaletteCoordinates,
		IUnsafePixelCollection<ushort> irPalettePixels,
		IDictionary<IMagickColor<ushort>, PaletteCoordinates> ck3ColorToCoordinatesDict
	) {
		var irColor = irPalettePixels.GetPixel(irPaletteCoordinates.X, irPaletteCoordinates.Y).ToColor();
		if (irColor is null) {
			Logger.Warn($"Cannot get color from palette {irPalettePixels}!");
			return new PaletteCoordinates();
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

	public void OutputGenes(StreamWriter output) {
		output.WriteLine("\t\tgenes={");

		foreach (var (geneName, geneValue) in DNAValues) {
			output.WriteLine($"\t\t\t{geneName}={{{geneValue}}}");
		}

		output.WriteLine("\t\t}");
	}
}