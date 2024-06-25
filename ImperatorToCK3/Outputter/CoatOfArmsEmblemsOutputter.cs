using commonItems;
using commonItems.Mods;
using ImageMagick;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ImperatorToCK3.Outputter;

public static class CoatOfArmsEmblemsOutputter {
	private static void ConvertColoredEmblems(string outputModPath, ModFilesystem imperatorModFS) {
		Logger.Info("Converting colored emblems...");
		var coloredEmblemsFolder = "gfx/coat_of_arms/colored_emblems";
		var acceptedExtensions = new HashSet<string>{ "dds", "tga", "png" };

		var emblemFiles = imperatorModFS.GetAllFilesInFolderRecursive(coloredEmblemsFolder);
		Parallel.ForEach(emblemFiles, filePath => {
			if (!acceptedExtensions.Contains(CommonFunctions.GetExtension(filePath))) {
				return;
			}
			CopyEmblem(filePath);
		});
		Logger.IncrementProgress();
		return;

		void CopyEmblem(string emblemFilePath) {
			var fileName = CommonFunctions.TrimPath(emblemFilePath);

			// Load an image.
			var image = new MagickImage(emblemFilePath);
			image.Negate(channels: Channels.Red);
			// Write the image to new file.
			var outputPath = Path.Combine(outputModPath, "gfx/coat_of_arms/colored_emblems", fileName);
			try {
				image.Write(outputPath);
			} catch (Exception ex) {
				Logger.Debug($"Exception occurred while writing {outputPath}: {ex}");
				Logger.Warn($"Failed to write colored emblem {fileName}. CoAs using this emblem will be broken.");
			}
		}
	}
	
	private static void CopyTexturedEmblems(string outputModPath, ModFilesystem imperatorModFS) {
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
			var outputPath = Path.Combine(outputModPath, "gfx/coat_of_arms/textured_emblems", fileName);
			var wasCopied = SystemUtils.TryCopyFile(filePath, outputPath);
			if (!wasCopied) {
				Logger.Warn($"Failed to copy textured emblem {fileName}!");
			}
		}
	}
	
	public static async Task CopyEmblems(string outputModPath, ModFilesystem imperatorModFS) {
		await Task.WhenAll(
			Task.Run(() => ConvertColoredEmblems(outputModPath, imperatorModFS)),
			Task.Run(() => CopyTexturedEmblems(outputModPath, imperatorModFS))
		);
	}
}
