using commonItems;
using ImageMagick;
using ImperatorToCK3.CommonUtils.Genes;
using ImperatorToCK3.Imperator.Characters;
using ImperatorToCK3.Mappers.Gene;
using System;
using System.Collections.Generic;
using System.IO;

namespace ImperatorToCK3.CK3.Characters;

public class DNA {
	public class PaletteCoordinates {
		// hair, skin and eye color palettes are 256x256
		public int X { get; set; } = 128;
		public int Y { get; set; } = 128;
	}

	public string Id { get; }
	public PaletteCoordinates HairCoordinates { get; set; } = new();
	public PaletteCoordinates HairCoordinates2 { get; set; } = new();
	public PaletteCoordinates SkinCoordinates { get; set; } = new();
	public PaletteCoordinates SkinCoordinates2 { get; set; } = new();
	public PaletteCoordinates EyeCoordinates { get; set; } = new();
	public PaletteCoordinates EyeCoordinates2 { get; set; } = new();

	private static IUnsafePixelCollection<ushort>? impHairPalettePixels;
	private static IUnsafePixelCollection<ushort>? ck3HairPalettePixels;

	private static IUnsafePixelCollection<ushort>? impSkinPalettePixels;
	private static IUnsafePixelCollection<ushort>? ck3SkinPalettePixels;

	private static IUnsafePixelCollection<ushort>? impEyePalettePixels;
	private static IUnsafePixelCollection<ushort>? ck3EyePalettePixels;

	private static GenesDB? genesDB;
	private static readonly AccessoryGeneMapper accessoryGeneMapper = new("configurables/accessory_genes_map.txt");
	public List<string> DNALines { get; } = new();

	public static void Initialize(Configuration config) {
		var impHairPalettePath = Path.Combine(config.ImperatorPath, "game/gfx/portraits/hair_palette.dds");
		impHairPalettePixels = new MagickImage(impHairPalettePath).GetPixelsUnsafe();
		var ck3HairPalettePath = Path.Combine(config.CK3Path, "game/gfx/portraits/hair_palette.dds");
		ck3HairPalettePixels = new MagickImage(ck3HairPalettePath).GetPixelsUnsafe();

		var impSkinPalettePath = Path.Combine(config.ImperatorPath, "game/gfx/portraits/skin_palette.dds");
		impSkinPalettePixels = new MagickImage(impSkinPalettePath).GetPixelsUnsafe();
		var ck3SkinPalettePath = Path.Combine(config.CK3Path, "game/gfx/portraits/skin_palette.dds");
		ck3SkinPalettePixels = new MagickImage(ck3SkinPalettePath).GetPixelsUnsafe();

		var impEyePalettePath = Path.Combine(config.ImperatorPath, "game/gfx/portraits/eye_palette.dds");
		impEyePalettePixels = new MagickImage(impEyePalettePath).GetPixelsUnsafe();
		var ck3EyePalettePath = Path.Combine(config.CK3Path, "game/gfx/portraits/eye_palette.dds");
		ck3EyePalettePixels = new MagickImage(ck3EyePalettePath).GetPixelsUnsafe();

		genesDB = new GenesDB(config.CK3Path, new List<Mod>());
	}
	public DNA(Imperator.Characters.Character impCharacter, Imperator.Characters.PortraitData impPortraitData) {
		Id = $"dna_{impCharacter.Id}";

		HairCoordinates = GetPaletteCoordinates(
			impPortraitData.HairColorPaletteCoordinates, impHairPalettePixels, ck3HairPalettePixels
		);
		HairCoordinates2 = GetPaletteCoordinates(
			impPortraitData.HairColor2PaletteCoordinates, impHairPalettePixels, ck3HairPalettePixels
		);
		var hairLine = $"hair_color={{{HairCoordinates.X} {HairCoordinates.Y} {HairCoordinates2.X} {HairCoordinates2.Y}}}";
		DNALines.Add(hairLine);

		SkinCoordinates = GetPaletteCoordinates(
			impPortraitData.SkinColorPaletteCoordinates, impSkinPalettePixels, ck3SkinPalettePixels
		);
		SkinCoordinates2 = GetPaletteCoordinates(
			impPortraitData.SkinColor2PaletteCoordinates, impSkinPalettePixels, ck3SkinPalettePixels
		);
		var skinLine = $"skin_color={{{SkinCoordinates.X} {SkinCoordinates.Y} {SkinCoordinates2.X} {SkinCoordinates2.Y}}}";
		DNALines.Add(skinLine);

		EyeCoordinates = GetPaletteCoordinates(
			impPortraitData.EyeColorPaletteCoordinates, impEyePalettePixels, ck3EyePalettePixels
		);
		EyeCoordinates2 = GetPaletteCoordinates(
			impPortraitData.EyeColor2PaletteCoordinates, impEyePalettePixels, ck3EyePalettePixels
		);
		var eyeLine = $"eye_color={{{EyeCoordinates.X} {EyeCoordinates.Y} {EyeCoordinates2.X} {EyeCoordinates2.Y}}}";
		DNALines.Add(eyeLine);

		ConvertAccessoryGene(impCharacter, impPortraitData, "beards", "beards", "scripted_character_beards_01");
	}

	private void ConvertAccessoryGene(
		Imperator.Characters.Character impCharacter,
		PortraitData impPortraitData,
		string imperatorGeneName,
		string ck3GeneName,
		string ck3GeneSetName
	) {
		if (!impPortraitData.AccessoryGenesDict.TryGetValue(imperatorGeneName, out var geneInfo)) {
			return;
		}
		if (genesDB is null) {
			Logger.Error("Cannot determine accessory genes: genes DB is uninitialized!");
			return;
		}
		var geneSet = genesDB.Genes[ck3GeneName].GeneTemplates[ck3GeneSetName];

		var convertedSetEntry = accessoryGeneMapper.BeardMappings[geneInfo.ObjectName];
		var convertedSetEntryRecessive = accessoryGeneMapper.BeardMappings[geneInfo.ObjectNameRecessive];

		var matchingPercentage = geneSet.AgeSexWeightBlocks[impCharacter.AgeSex].GetMatchingPercentage(convertedSetEntry);
		var matchingPercentageRecessive = geneSet.AgeSexWeightBlocks[impCharacter.AgeSex].GetMatchingPercentage(convertedSetEntryRecessive);
		int intSliderValue = (int)Math.Ceiling(matchingPercentage * 255);
		int intSliderValueRecessive = (int)Math.Ceiling(matchingPercentageRecessive * 255);

		var dnaLine = $"{ck3GeneName}={{ \"{ck3GeneSetName}\" {intSliderValue} \"{ck3GeneSetName}\" {intSliderValueRecessive} }}";
		DNALines.Add(dnaLine);
	}

	private static PaletteCoordinates GetPaletteCoordinates(
		Imperator.Characters.PaletteCoordinates impPaletteCoordinates,
		IUnsafePixelCollection<ushort>? impPalettePixels,
		IUnsafePixelCollection<ushort>? ck3PalettePixels
	) {
		if (impPalettePixels is null) {
			throw new ArgumentNullException(nameof(impPalettePixels));
		}
		if (ck3PalettePixels is null) {
			throw new ArgumentNullException(nameof(ck3PalettePixels));
		}

		var bestCoordinates = new PaletteCoordinates();

		var impColor = impPalettePixels.GetPixel(impPaletteCoordinates.X, impPaletteCoordinates.Y).ToColor();
		if (impColor is null) {
			Logger.Warn($"Cannot get color from palette {impPalettePixels}!");
			return bestCoordinates;
		}

		var minColorDistance = double.MaxValue;
		foreach (var pixel in ck3PalettePixels) {
			var color = pixel.ToColor();
			if (color is null) {
				continue;
			}
			var rDiff = Math.Abs(impColor.R - color.R);
			var gDiff = Math.Abs(impColor.G - color.G);
			var bDiff = Math.Abs(impColor.B - color.B);
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