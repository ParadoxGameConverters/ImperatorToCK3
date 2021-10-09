using ImageMagick;
using System;
using System.IO;
using commonItems;

namespace ImperatorToCK3.CK3.Characters {
	public class DNA {
		public class PaletteCoordinates {
			// hair, skin and eye color palettes are 256x256
			public int x = 128;
			public int y = 128;
		}

		public string Id { get; }
		public PaletteCoordinates HairCoordinates { get; set; } = new();
		public PaletteCoordinates HairCoordinates2 { get; set; } = new();
		public PaletteCoordinates SkinCoordinates { get; set; } = new();
		public PaletteCoordinates SkinCoordinates2 { get; set; } = new();
		public PaletteCoordinates EyeCoordinates { get; set; } = new();
		public PaletteCoordinates EyeCoordinates2 { get; set; } = new();

		public DNA(string characterId, Imperator.Characters.PortraitData impPortraitData, Configuration config) {
			Id = "dna_" + characterId;
			var impHairPalettePath = Path.Combine(config.ImperatorPath, "game/gfx/portraits/hair_palette.dds");
			var ck3HairPalettePath = Path.Combine(config.Ck3Path, "game/gfx/portraits/hair_palette.dds");
			HairCoordinates = GetPaletteCoordinates(
				impPortraitData.HairColorPaletteCoordinates,
				impHairPalettePath,
				ck3HairPalettePath
			);
			HairCoordinates2 = GetPaletteCoordinates(
				impPortraitData.HairColor2PaletteCoordinates,
				impHairPalettePath,
				ck3HairPalettePath
			);

			var impSkinPalettePath = Path.Combine(config.ImperatorPath, "game/gfx/portraits/skin_palette.dds");
			var ck3SkinPalettePath = Path.Combine(config.Ck3Path, "game/gfx/portraits/skin_palette.dds");
			SkinCoordinates = GetPaletteCoordinates(
				impPortraitData.SkinColorPaletteCoordinates,
				impSkinPalettePath,
				ck3SkinPalettePath
			);
			SkinCoordinates2 = GetPaletteCoordinates(
				impPortraitData.SkinColor2PaletteCoordinates,
				impSkinPalettePath,
				ck3SkinPalettePath
			);

			var impEyePalettePath = Path.Combine(config.ImperatorPath, "game/gfx/portraits/eye_palette.dds");
			var ck3EyePalettePath = Path.Combine(config.Ck3Path, "game/gfx/portraits/eye_palette.dds");
			EyeCoordinates = GetPaletteCoordinates(
				impPortraitData.EyeColorPaletteCoordinates,
				impEyePalettePath,
				ck3EyePalettePath
			);
			EyeCoordinates2 = GetPaletteCoordinates(
				impPortraitData.EyeColor2PaletteCoordinates,
				impEyePalettePath,
				ck3EyePalettePath
			);
		}
		private static PaletteCoordinates GetPaletteCoordinates(
			Imperator.Characters.PaletteCoordinates impPaletteCoordinates,
			string impPalettePath,
			string ck3PalettePath
		) {
			var bestCoordinates = new PaletteCoordinates();

			using var impPalette = new MagickImage(impPalettePath);
			var impColor = impPalette.GetPixelsUnsafe().GetPixel(impPaletteCoordinates.x, impPaletteCoordinates.y).ToColor();
			if (impColor is null) {
				Logger.Warn($"Cannot get color from palette {impPalettePath}!");
				return bestCoordinates;
			}

			using (var image = new MagickImage(ck3PalettePath)) {
				var minColorDistance = double.MaxValue;
				foreach (var pixel in image.GetPixelsUnsafe()) {
					var color = pixel.ToColor();
					if (color is null) {
						continue;
					}
					var rDiff = Math.Abs(impColor.R - color.R);
					var gDiff = Math.Abs(impColor.G - color.G);
					var bDiff = Math.Abs(impColor.B - color.B);
					double colorDistance = Math.Pow(rDiff, 3) + Math.Pow(gDiff, 3) + Math.Pow(bDiff, 3);

					if (colorDistance < minColorDistance) {
						bestCoordinates = new() { x = pixel.X, y = pixel.Y };
						minColorDistance = colorDistance;
						if (minColorDistance == 0) {
							return bestCoordinates;
						}
					}
				}
			}

			return bestCoordinates;
		}
	}
}
