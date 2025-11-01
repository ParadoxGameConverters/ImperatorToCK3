using commonItems;
using commonItems.Mods;
using ImageMagick;
using System;
using System.Collections.Frozen;
using System.IO;
using System.Threading.Tasks;

namespace ImperatorToCK3.Outputter;

internal static class CoatOfArmsEmblemsOutputter {
	private static readonly FrozenSet<string> acceptedEmblemExtensions = new[] {"dds", "tga", "png"}.ToFrozenSet();

	private static void ConvertColoredEmblems(string outputModPath, ModFilesystem imperatorModFS) {
		Logger.Info("Converting colored emblems...");
		const string coloredEmblemsFolder = "gfx/coat_of_arms/colored_emblems";

		var emblemFiles = imperatorModFS.GetAllFilesInFolderRecursive(coloredEmblemsFolder);
		Parallel.ForEach(emblemFiles, fileInfo => {
			if (!acceptedEmblemExtensions.Contains(CommonFunctions.GetExtension(fileInfo.RelativePath))) {
				return;
			}
			CopyEmblem(outputModPath, fileInfo.AbsolutePath);
		});
		Logger.IncrementProgress();
	}

	private static void CopyEmblem(string outputModPath, string emblemFilePath) {
		var fileName = CommonFunctions.TrimPath(emblemFilePath);

		// Load an image.
		MagickImage image;
		try {
			image = new(emblemFilePath);
			image.Negate(channels: Channels.Red);
		} catch (Exception ex) {
			Logger.Debug($"Exception occurred while loading {emblemFilePath}: {ex}");
			Logger.Warn($"Failed to load colored emblem {fileName}. CoAs using this emblem will be broken.");
			return;
		}

		// Write the image to new file.
		var outputPath = Path.Combine(outputModPath, "gfx/coat_of_arms/colored_emblems", fileName);
		try {
			image.Write(outputPath);
		} catch (Exception ex) {
			Logger.Debug($"Exception occurred while writing {outputPath}: {ex}");
			Logger.Warn($"Failed to write colored emblem {fileName}. CoAs using this emblem will be broken.");
		}
	}

	private static void CopyTexturedEmblems(string outputModPath, ModFilesystem imperatorModFS) {
		Logger.Info("Copying textured emblems...");
		const string texturedEmblemsFolder = "gfx/coat_of_arms/textured_emblems";

		var emblemFiles = imperatorModFS.GetAllFilesInFolderRecursive(texturedEmblemsFolder);
		foreach (var fileInfo in emblemFiles) {
			if (!acceptedEmblemExtensions.Contains(CommonFunctions.GetExtension(fileInfo.RelativePath))) {
				continue;
			}
			
			// Copy image to output path.
			var outputPath = Path.Combine(outputModPath, texturedEmblemsFolder, fileInfo.RelativePath);
			var wasCopied = SystemUtils.TryCopyFile(fileInfo.AbsolutePath, outputPath);
			if (!wasCopied) {
				Logger.Warn($"Failed to copy textured emblem {fileInfo.RelativePath}!");
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
