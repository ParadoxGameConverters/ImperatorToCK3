using commonItems;
using ImageMagick;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ImperatorToCK3.Outputter;

public static class ColoredEmblemsOutputter {
	private static bool IsBrokenEmblem(string fileName, Configuration config) {
		var emblemFilePath = Path.Combine(
			"output",
			config.OutputModName,
			"gfx", "coat_of_arms", "colored_emblems", "ce_lion.dds"
		);
		// something's wrong with ce_lion.dds
		if (fileName == "ce_lion.dds" && !File.Exists(emblemFilePath)) {
			// instead of converting a broken file from Imperator, copy closest CK3 emblem
			var wasCopied = SystemUtils.TryCopyFile(
				Path.Combine(config.CK3Path, "game", "gfx", "coat_of_arms", "colored_emblems", "ce_lion_passant.dds"),
				emblemFilePath
			);
			if (!wasCopied) {
				Logger.Warn("Couldn't copy a replacement for ce_lion.dds!");
			}
			return true;
		}
		return false;
	}

	public static void CopyColoredEmblems(Configuration config, IEnumerable<Mod> mods) {
		var coloredEmblemsFolder = Path.Combine("gfx", "coat_of_arms", "colored_emblems");
		var vanillaEmblemsFolder = Path.Combine(config.ImperatorPath, "game", coloredEmblemsFolder);
		var acceptedExtensions = new HashSet<string>{ "dds", "tga", "png" };
		foreach (var fileName in SystemUtils.GetAllFilesInFolderRecursive(vanillaEmblemsFolder)) {
			if (!acceptedExtensions.Contains(CommonFunctions.GetExtension(fileName))) {
				continue;
			}
			CopyEmblem(vanillaEmblemsFolder, fileName);
		}

		foreach (var modEmblemsFolder in mods.Select(mod => Path.Combine(mod.Path, coloredEmblemsFolder))) {
			foreach (var fileName in SystemUtils.GetAllFilesInFolderRecursive(modEmblemsFolder)) {
				if (!acceptedExtensions.Contains(CommonFunctions.GetExtension(fileName))) {
					continue;
				}
				CopyEmblem(modEmblemsFolder, fileName);
			}
		}

		void CopyEmblem(string emblemsFolderPath, string emblemsFileName) {
			var emblemsFilePath = Path.Combine(emblemsFolderPath, emblemsFileName);
			if (IsBrokenEmblem(emblemsFileName, config)) {
				return;
			}
			// Load an image.
			var image = new MagickImage(emblemsFilePath);
			image.Negate(channels: Channels.Red);
			// Write the image to new file.
			var outputPath = Path.Combine("output", config.OutputModName, "gfx", "coat_of_arms", "colored_emblems", emblemsFileName);
			image.Write(outputPath);
		}
	}
}
