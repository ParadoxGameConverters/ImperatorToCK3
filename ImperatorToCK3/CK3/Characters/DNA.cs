using commonItems;
using ImageMagick;
using ImperatorToCK3.CommonUtils.Genes;
using ImperatorToCK3.Mappers.Gene;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ImperatorToCK3.CK3.Characters {
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
			var ck3HairPalettePath = Path.Combine(config.Ck3Path, "game/gfx/portraits/hair_palette.dds");
			ck3HairPalettePixels = new MagickImage(ck3HairPalettePath).GetPixelsUnsafe();

			var impSkinPalettePath = Path.Combine(config.ImperatorPath, "game/gfx/portraits/skin_palette.dds");
			impSkinPalettePixels = new MagickImage(impSkinPalettePath).GetPixelsUnsafe();
			var ck3SkinPalettePath = Path.Combine(config.Ck3Path, "game/gfx/portraits/skin_palette.dds");
			ck3SkinPalettePixels = new MagickImage(ck3SkinPalettePath).GetPixelsUnsafe();

			var impEyePalettePath = Path.Combine(config.ImperatorPath, "game/gfx/portraits/eye_palette.dds");
			impEyePalettePixels = new MagickImage(impEyePalettePath).GetPixelsUnsafe();
			var ck3EyePalettePath = Path.Combine(config.Ck3Path, "game/gfx/portraits/eye_palette.dds");
			ck3EyePalettePixels = new MagickImage(ck3EyePalettePath).GetPixelsUnsafe();

			var genesPath = Path.Combine(config.Ck3Path, "game/common/genes/04_genes_special_accessories_beards.txt");
			genesDB = new GenesDB(genesPath);
		}
		public DNA(Imperator.Characters.Character impCharacter, Imperator.Characters.PortraitData impPortraitData) {
			Id = "dna_" + impCharacter.ID;

			HairCoordinates = GetPaletteCoordinates(
				impPortraitData.HairColorPaletteCoordinates, impHairPalettePixels, ck3HairPalettePixels
			);
			HairCoordinates2 = GetPaletteCoordinates(
				impPortraitData.HairColor2PaletteCoordinates, impHairPalettePixels, ck3HairPalettePixels
			);

			SkinCoordinates = GetPaletteCoordinates(
				impPortraitData.SkinColorPaletteCoordinates, impSkinPalettePixels, ck3SkinPalettePixels
			);
			SkinCoordinates2 = GetPaletteCoordinates(
				impPortraitData.SkinColor2PaletteCoordinates, impSkinPalettePixels, ck3SkinPalettePixels
			);

			EyeCoordinates = GetPaletteCoordinates(
				impPortraitData.EyeColorPaletteCoordinates, impEyePalettePixels, ck3EyePalettePixels
			);
			EyeCoordinates2 = GetPaletteCoordinates(
				impPortraitData.EyeColor2PaletteCoordinates, impEyePalettePixels, ck3EyePalettePixels
			);

			const string geneSetName = "all_beards";
			if (genesDB is null) {
				Logger.Error("Cannot determine accessory genes: genes DB is uninitialized!");
			} else {
				if (!impPortraitData.AccessoryGenesDict.TryGetValue("beards", out var geneInfo)) {
					return;
				}
				var impSetEntry = geneInfo.objectName;
				var convertedSetEntry = accessoryGeneMapper.BeardMappings[impSetEntry];

				var geneSet = genesDB.Genes.Genes["beards"].GeneTemplates[geneSetName];
				var ageSex = impCharacter.AgeSex;
				var matchingPercentage = geneSet.AgeSexWeightBlocks[ageSex].GetMatchingPercentage(convertedSetEntry);
				int intSliderValue = (int)(Math.Ceiling(matchingPercentage) * 255);

				// currently only dominant entry for the beard gene is outputted (twice)
				// TODO: convert recessive entries
				var dnaLine = $"beards={{{convertedSetEntry} {intSliderValue} {convertedSetEntry} {intSliderValue}}}";
				DNALines.Add(dnaLine);
			}
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

			var impColor = impPalettePixels.GetPixel(impPaletteCoordinates.x, impPaletteCoordinates.y).ToColor();
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
	}
}
