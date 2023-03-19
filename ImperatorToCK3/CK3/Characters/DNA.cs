using commonItems;
using commonItems.Mods;
using ImageMagick;
using ImperatorToCK3.CommonUtils.Genes;
using ImperatorToCK3.Exceptions;
using ImperatorToCK3.Imperator.Characters;
using ImperatorToCK3.Mappers.Gene;
using System;
using System.Collections.Generic;
using System.IO;

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
	public List<string> DNALines { get; } = new();

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
			irPortraitData.HairColorPaletteCoordinates, irHairPalettePixels, ck3HairPalettePixels
		);
		HairCoordinates2 = GetPaletteCoordinates(
			irPortraitData.HairColor2PaletteCoordinates, irHairPalettePixels, ck3HairPalettePixels
		);
		var hairLine = $"hair_color={{{HairCoordinates.X} {HairCoordinates.Y} {HairCoordinates2.X} {HairCoordinates2.Y}}}";
		DNALines.Add(hairLine);

		SkinCoordinates = GetPaletteCoordinates(
			irPortraitData.SkinColorPaletteCoordinates, irSkinPalettePixels, ck3SkinPalettePixels
		);
		SkinCoordinates2 = GetPaletteCoordinates(
			irPortraitData.SkinColor2PaletteCoordinates, irSkinPalettePixels, ck3SkinPalettePixels
		);
		var skinLine = $"skin_color={{{SkinCoordinates.X} {SkinCoordinates.Y} {SkinCoordinates2.X} {SkinCoordinates2.Y}}}";
		DNALines.Add(skinLine);

		EyeCoordinates = GetPaletteCoordinates(
			irPortraitData.EyeColorPaletteCoordinates, irEyePalettePixels, ck3EyePalettePixels
		);
		EyeCoordinates2 = GetPaletteCoordinates(
			irPortraitData.EyeColor2PaletteCoordinates, irEyePalettePixels, ck3EyePalettePixels
		);
		var eyeLine = $"eye_color={{{EyeCoordinates.X} {EyeCoordinates.Y} {EyeCoordinates2.X} {EyeCoordinates2.Y}}}";
		DNALines.Add(eyeLine);

		ConvertAccessoryGene(irCharacter, irPortraitData, "beards", "beards", "scripted_character_beards_01");
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

		var dnaLine = $"{ck3GeneName}={{ \"{ck3GeneSetName}\" {intSliderValue} \"{ck3GeneSetName}\" {intSliderValueRecessive} }}";
		DNALines.Add(dnaLine);
	}

	private static PaletteCoordinates GetPaletteCoordinates(
		Imperator.Characters.PaletteCoordinates irPaletteCoordinates,
		IUnsafePixelCollection<ushort>? irPalettePixels,
		IUnsafePixelCollection<ushort>? ck3PalettePixels
	) {
		if (irPalettePixels is null) {
			throw new ArgumentNullException(nameof(irPalettePixels));
		}
		if (ck3PalettePixels is null) {
			throw new ArgumentNullException(nameof(ck3PalettePixels));
		}

		var bestCoordinates = new PaletteCoordinates();

		var irColor = irPalettePixels.GetPixel(irPaletteCoordinates.X, irPaletteCoordinates.Y).ToColor();
		if (irColor is null) {
			Logger.Warn($"Cannot get color from palette {irPalettePixels}!");
			return bestCoordinates;
		}

		var minColorDistance = double.MaxValue;
		foreach (var pixel in ck3PalettePixels) {
			var color = pixel.ToColor();
			if (color is null) {
				continue;
			}
			var rDiff = Math.Abs(irColor.R - color.R);
			var gDiff = Math.Abs(irColor.G - color.G);
			var bDiff = Math.Abs(irColor.B - color.B);
			double colorDistance = Math.Pow(rDiff, 3) + Math.Pow(gDiff, 3) + Math.Pow(bDiff, 3);

			if (colorDistance < minColorDistance) {
				bestCoordinates = new() { X = pixel.X, Y = pixel.Y };
				minColorDistance = colorDistance;
				if (minColorDistance == 0) {
					return bestCoordinates;
				}
			}
		}

		return bestCoordinates;
	}

	public void OutputGenes(StreamWriter output) {
		output.WriteLine("\t\tgenes={");

		foreach (var line in DNALines) {
			output.WriteLine("\t\t\t" + line);
		}

		output.WriteLine("\t\t}");
	}
}