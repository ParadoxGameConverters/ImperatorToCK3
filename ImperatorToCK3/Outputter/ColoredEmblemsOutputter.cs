using commonItems;
using ImageMagick;
using System.IO;

namespace ImperatorToCK3.Outputter {
	public static class ColoredEmblemsOutputter {
		private static bool IsBrokenEmblem(Configuration configuration, string outputName, string fileName) {
			var emblemFilePath = Path.Combine(
				"output",
				outputName,
				"gfx/coat_of_arms/colored_emblems/ce_lion.dds"
			);
			if (fileName == "ce_lion.dds" && !File.Exists(emblemFilePath)) { // something's wrong with ce_lion.dds
																			 // instead of converting a broken file from Imperator, copy closest CK3 emblem
				SystemUtils.TryCopyFile(
					Path.Combine(configuration.CK3Path, "game/gfx/coat_of_arms/colored_emblems/ce_lion_passant.dds"),
					emblemFilePath
				);
				return true;
			}
			return false;
		}

		public static void CopyColoredEmblems(Configuration configuration, string outputName) {
			var coloredEmblemsPath = Path.Combine(
				configuration.ImperatorPath,
				"game/gfx/coat_of_arms/colored_emblems"
			);
			var filenames = SystemUtils.GetAllFilesInFolderRecursive(coloredEmblemsPath);
			foreach (var filename in filenames) {
				if (IsBrokenEmblem(configuration, outputName, filename)) {
					continue;
				}
				// Load an image.
				var filePath = Path.Combine(coloredEmblemsPath, filename);
				var image = new MagickImage(filePath);
				image.Negate(channels: Channels.Red);
				// Write the image to new file.
				var outputPath = Path.Combine("output", outputName, "gfx/coat_of_arms/colored_emblems", filename);
				image.Write(outputPath);
			}
		}
	}
}
