using commonItems;
using commonItems.Mods;
using ImageMagick;
using System.Collections.Generic;
using System.IO;

namespace ImperatorToCK3.Outputter;

public static class CoatOfArmsEmblemsOutputter {
	private static bool IsBrokenColoredEmblem(string fileName, Configuration config) {
		var emblemFilePath = Path.Combine(
			"output",
			config.OutputModName,
			"gfx/coat_of_arms/colored_emblems/ce_lion.dds"
		);
		// Something's wrong with ce_lion.dds.
		if (fileName == "ce_lion.dds" && !File.Exists(emblemFilePath)) {
			// Instead of converting a broken file from Imperator, copy closest CK3 emblem.
			var wasCopied = SystemUtils.TryCopyFile(
				Path.Combine(config.CK3Path, "game/gfx/coat_of_arms/colored_emblems/ce_lion_passant.dds"),
				emblemFilePath
			);
			if (!wasCopied) {
				Logger.Warn("Couldn't copy a replacement for ce_lion.dds!");
			}
			return true;
		}
		return false;
	}

	private static void CopyColoredEmblems(Configuration config, ModFilesystem imperatorModFS) {
		Logger.Info("Copying colored emblems...");
		var coloredEmblemsFolder = "gfx/coat_of_arms/colored_emblems";
		var acceptedExtensions = new HashSet<string>{ "dds", "tga", "png" };

		var emblemFiles = imperatorModFS.GetAllFilesInFolderRecursive(coloredEmblemsFolder);
		foreach (var filePath in emblemFiles) {
			if (!acceptedExtensions.Contains(CommonFunctions.GetExtension(filePath))) {
				continue;
			}
			CopyEmblem(filePath);
		}
		Logger.IncrementProgress();

		void CopyEmblem(string emblemFilePath) {
			var fileName = CommonFunctions.TrimPath(emblemFilePath);

			if (IsBrokenColoredEmblem(fileName, config)) {
				return;
			}
			// Load an image.
			var image = new MagickImage(emblemFilePath);
			image.Negate(channels: Channels.Red);
			// Write the image to new file.
			var outputPath = Path.Combine("output", config.OutputModName, "gfx/coat_of_arms/colored_emblems", fileName);
			image.Write(outputPath);
		}
	}
	
	private static void CopyTexturedEmblems(Configuration config, ModFilesystem imperatorModFS) {
		Logger.Info("Copying textured emblems...");
		const string texturedEmblemsFolder = "gfx/coat_of_arms/textured_emblems";
		var acceptedExtensions = new HashSet<string>{ "dds", "tga", "png" };

		var emblemFiles = imperatorModFS.GetAllFilesInFolderRecursive(texturedEmblemsFolder);
		foreach (var filePath in emblemFiles) {
			if (!acceptedExtensions.Contains(CommonFunctions.GetExtension(filePath))) {
				continue;
			}
			
			// Copy image to output path.
			var fileName = CommonFunctions.TrimPath(filePath);
			var outputPath = Path.Combine("output", config.OutputModName, "gfx/coat_of_arms/textured_emblems", fileName);
			var wasCopied = SystemUtils.TryCopyFile(filePath, outputPath);
			if (!wasCopied) {
				Logger.Warn($"Failed to copy textured emblem {fileName}!");
			}
		}
	}
	
	public static void CopyEmblems(Configuration config, ModFilesystem imperatorModFS) {
		CopyColoredEmblems(config, imperatorModFS);
		CopyTexturedEmblems(config, imperatorModFS);
	}
}
